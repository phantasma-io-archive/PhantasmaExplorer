using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LunarLabs.Parser;
using LunarLabs.Parser.XML;
using Phantasma.Core.Types;
using Phantasma.Cryptography;
using Phantasma.Domain;
using Phantasma.Numerics;
using Phantasma.VM;

namespace Phantasma.Explorer
{
    public abstract class ExplorerObject
    {
        public readonly NexusData Nexus;

        public ExplorerObject(NexusData database)
        {
            this.Nexus = database;
        }
    }

    public enum SearchResultKind
    {
        Address,
        Transaction,
        Block,
        Token,
        Organization,
        Leaderboard,
        Contract,
        Platform,
        Chain,

    }

    public struct SearchResult
    {
        public SearchResultKind Kind;
        public string Text;
        public string Data;
    }

    public struct OracleData
    {
        public string URL;
        public string Content;
    }

    public class BlockData : ExplorerObject
    {
        public Address ChainAddress { get; private set; }
        public BigInteger Height { get; private set; }
        public Timestamp Timestamp { get; private set; }
        public Hash PreviousHash { get; private set; }
        public uint Protocol { get; private set; }
        public Hash Hash { get; private set; }
        public Hash[] TransactionHashes { get; private set; }
        public OracleData[] OracleData { get; private set; }

        public Event[] Events { get; private set; }

        public Address ValidatorAddress { get; private set; }

        public TransactionData[] Transactions { get; private set; }

        public ChainData Chain { get; private set; }

        public DateTime Date => (DateTime)Timestamp;

        public BlockData(NexusData database, DataNode node) : base(database)
        {
            ChainAddress = Address.FromText(node.GetString("chainAddress"));
            Chain = Nexus.FindChainByAddress(this.ChainAddress);

            Height = BigInteger.Parse(node.GetString("height"));
            Timestamp = new Timestamp(node.GetUInt32("timestamp"));
            PreviousHash = Hash.Parse(node.GetString("previousHash"));
            Protocol = node.GetUInt32("protocol");
            Hash = Hash.Parse(node.GetString("hash"));

            Events = Nexus.ReadEvents(node);

            ValidatorAddress = Address.FromText(node.GetString("validatorAddress"));

            var oracleNode = node.GetNode("oracles");
            if (oracleNode != null)
            {
                OracleData = new OracleData[oracleNode.ChildCount];
                for (int i = 0; i < OracleData.Length; i++)
                {
                    var temp = oracleNode.GetNodeByIndex(i);
                    OracleData[i] = new OracleData()
                    {
                        Content = temp.GetString("content"),
                        URL = temp.GetString("url"),
                    };
                }
            }
            else
            {
                OracleData = new OracleData[0];
            }

            Nexus.RegisterSearch(this.Hash.ToString(), null, SearchResultKind.Block);

            var txsNode = node.GetNode("txs");
            TransactionHashes = new Hash[txsNode.ChildCount];
            Transactions = new TransactionData[TransactionHashes.Length];

            var missingHashes = new List<Hash>();

            for (int i=0; i<TransactionHashes.Length; i++)
            {
                var txNode = txsNode.GetNodeByIndex(i);
                var txHash = Hash.Parse(txNode.GetString("hash"));
                TransactionHashes[i] = txHash;

                var fileName = Nexus.GetTransactionCacheFileName(txHash);
                if (File.Exists(fileName))
                {
                    var xml = File.ReadAllText(fileName);
                    var temp = XMLReader.ReadFromString(xml);
                    temp = temp.GetNodeByIndex(0);
                    var tx = new TransactionData(Nexus, temp);
                    Transactions[i] = tx;
                    Console.WriteLine("Loaded transaction from cache: " + txHash);
                }
                else
                {
                    missingHashes.Add(txHash);
                }
            }

            Nexus.DoParallelRequests($"Fetching missing transactions for block {Hash}...", missingHashes.Count, false, (index) =>
            {
                var hash = missingHashes[index];
                var tx = Nexus.FindTransaction(Chain, hash);
                
                for (int i=0; i<TransactionHashes.Length; i++)
                {
                    if (TransactionHashes[i] == hash)
                    {
                        Transactions[i] = tx;
                        break;
                    }

                }
            });
        }

        // Register all suitable data from Block in search database.
        public void RegisterBlockContents()
        {
            // Extract events from block's transactions.
            foreach (var tx in Transactions)
            {
                foreach (var evnt in tx.Events)
                {
                    RegisterEvent(evnt, false, tx.Hash);
                }
            }

            // Extract events from block itself.
            foreach (var evnt in Events)
            {
                RegisterEvent(evnt, true, this.Hash);
            }
        }

        // Register all suitable data from Event in search database.
        public void RegisterEvent(Event evnt, bool isBlockEvent, Hash hash)
        {
            if (!isBlockEvent)
            {
                // Register transaction search by hash.
                Nexus.RegisterSearch(hash.ToString(), null, SearchResultKind.Transaction);
            }

            if (evnt.Address.IsUser)
            {
                if (!isBlockEvent)
                {
                    // Register transaction search by address.
                    Nexus.RegisterSearch(evnt.Address.Text, hash.ToString(), SearchResultKind.Transaction, hash.ToString());
                }

                // Register address search.
                Nexus.RegisterSearch(evnt.Address.Text, null, SearchResultKind.Address);
            }

            switch (evnt.Kind)
            {
                case EventKind.ChainCreate:
                    {
                        var name = evnt.GetContent<string>();
                        Nexus.RegisterSearch(name, "Chain Creation", SearchResultKind.Transaction, hash.ToString());
                        break;
                    }

                case EventKind.TokenCreate:
                    {
                        var symbol = evnt.GetContent<string>();
                        Nexus.RegisterSearch(symbol, "Token Creation", SearchResultKind.Transaction, hash.ToString());
                        break;
                    }

                case EventKind.AddressRegister:
                    {
                        var name = evnt.GetContent<string>();
                        Nexus.RegisterSearch(name, "Name Registration", SearchResultKind.Transaction, hash.ToString());

                        // Register address search by name.
                        Nexus.RegisterSearch(name, null, SearchResultKind.Address, evnt.Address.ToString());
                        break;
                    }

                case EventKind.ContractDeploy:
                    {
                        var name = evnt.GetContent<string>();
                        Nexus.RegisterSearch(name, "Deployment", SearchResultKind.Transaction, hash.ToString());
                        break;
                    }

                case EventKind.PlatformCreate:
                    {
                        var name = evnt.GetContent<string>();
                        Nexus.RegisterSearch(name, "Platform Creation", SearchResultKind.Transaction, hash.ToString());
                        break;
                    }

                case EventKind.OrganizationCreate:
                    {
                        var name = evnt.GetContent<string>();
                        Nexus.RegisterSearch(name, "Organization Creation", SearchResultKind.Transaction, hash.ToString());
                        break;
                    }

                case EventKind.ChainSwap:
                    {
                        var data = evnt.GetContent<TransactionSettleEventData>();
                        Nexus.RegisterSearch(data.Hash.ToString(), "Settlement", SearchResultKind.Transaction, hash.ToString());
                        break;
                    }

                case EventKind.ValueCreate:
                    {
                        var data = evnt.GetContent<ChainValueEventData>();
                        Nexus.RegisterSearch(data.Name, "Value Creation", SearchResultKind.Transaction, hash.ToString());
                        break;
                    }

                case EventKind.ValueUpdate:
                    {
                        var data = evnt.GetContent<ChainValueEventData>();
                        Nexus.RegisterSearch(data.Name, "Value Update", SearchResultKind.Transaction, hash.ToString());
                        break;
                    }

                case EventKind.TokenMint:
                    {
                        var data = evnt.GetContent<TokenEventData>();
                        Nexus.RegisterSearch(data.Symbol, "Token Mint", SearchResultKind.Transaction, hash.ToString());
                        break;
                    }

                case EventKind.TokenBurn:
                    {
                        var data = evnt.GetContent<TokenEventData>();
                        Nexus.RegisterSearch(data.Symbol, "Token Burn", SearchResultKind.Transaction, hash.ToString());
                        break;
                    }
            }
        }
    }

    public class TransactionData : ExplorerObject, ITransaction
    {
        public TransactionData(NexusData database, DataNode node) : base(database)
        {
            Script = Base16.Decode(node.GetString("script"));
            NexusName = null; // TODO
            ChainAddress = Address.FromText(node.GetString("chainAddress"));
            BlockHash = Hash.Parse(node.GetString("blockHash"));
            Expiration = 0; // TODO
            Payload = null; // TODO
            Signatures = null; // TODO
            Hash = Hash.Parse(node.GetString("hash"));

            Timestamp = new Timestamp(node.GetUInt32("timestamp"));

            Result = node.GetString("result");
            if (string.IsNullOrEmpty(Result))
            {
                Result = "-";
            }

            Events = Nexus.ReadEvents(node);

            var disasm = new Disassembler(this.Script);
            this.Instructions = disasm.Instructions.ToArray();
        }

        public ChainData Chain => Nexus.FindChainByAddress(ChainAddress);
        public string ChainName => Chain.Name;
        public BlockData Block => Nexus.FindBlockByHash(Chain, BlockHash);

        public byte[] Script { get; private set; }
        public string NexusName { get; private set; }
        public Address ChainAddress { get; private set; }
        public Hash BlockHash { get; private set; }
        public Timestamp Expiration { get; private set; }
        public byte[] Payload { get; private set; }
        public Signature[] Signatures { get; private set; }
        public Hash Hash { get; private set; }
        public Event[] Events { get; private set; }

        public string Result { get; private set; }

        public Timestamp Timestamp { get; private set; }
        public DateTime Date => (DateTime)Timestamp;

        public Instruction[] Instructions { get; set; }

        private string _description = null;
        public string Description {
            get
            {
                if (_description == null)
                {
                    _description = GenerateDescription();
                }

                return _description;
            }
        }

        private string LinkAddress(Address address, string name = null)
        {
            return LinkAddressTag.GenerateLink(this.Nexus, address, name);
        }

        private string LinkToken(string symbol)
        {
            var token = Nexus.FindTokenBySymbol(symbol);

            var name = token != null ? token.Name : symbol;

            return $"<a href=\"/token/{symbol}\">{symbol}</a>";
        }

        private string GenerateDescription()
        {
            decimal totalFees = 0;
            var fees = new Dictionary<Address, decimal>();

            Address feeAddress = Address.Null;

            var addresses = new HashSet<Address>();

            var sb = new StringBuilder();
            foreach (var evt in Events)
            {
                if (evt.Address.IsUser)
                {
                    addresses.Add(evt.Address);
                }

                if (evt.Contract == "gas")
                {
                    switch (evt.Kind)
                    {
                        case EventKind.TokenStake:
                            {
                                var data = evt.GetContent<TokenEventData>();
                                if (data.Symbol == DomainSettings.FuelTokenSymbol)
                                {
                                    feeAddress = evt.Address;
                                }
                                break;
                            }

                        case EventKind.TokenReceive:
                            {
                                var data = evt.GetContent<TokenEventData>();
                                if (data.Symbol == DomainSettings.FuelTokenSymbol)
                                {
                                    if (evt.Address.IsSystem)
                                    {
                                        var amount = UnitConversion.ToDecimal(data.Value, DomainSettings.FuelTokenDecimals);
                                        totalFees += amount;

                                        decimal fee = fees.ContainsKey(evt.Address) ? fees[evt.Address] : 0;
                                        fee += amount;

                                        fees[evt.Address] = fee;
                                    }
                                }
                                break;
                            }
                    }

                    continue;
                }

                switch (evt.Kind)
                {
                    case EventKind.ChainCreate:
                        {
                            var name = evt.GetContent<string>();
                            sb.AppendLine($"Created chain: <a href=\"/chain/{name}\">{name}</a>");
                            Nexus.RegisterSearch(name, "Chain Creation", SearchResultKind.Transaction, this.Hash.ToString());
                            break;
                        }

                    case EventKind.TokenCreate:
                        {
                            var symbol = evt.GetContent<string>();
                            sb.AppendLine($"Created token: {LinkToken(symbol)}");
                            Nexus.RegisterSearch(symbol, "Token Creation", SearchResultKind.Transaction, this.Hash.ToString());
                            break;
                        }

                    case EventKind.AddressRegister:
                        {
                            var name = evt.GetContent<string>();
                            sb.AppendLine($"Registered name: {name} for {LinkAddress(evt.Address, null)}");
                            Nexus.RegisterSearch(name, "Name Registration", SearchResultKind.Transaction, this.Hash.ToString());
                            break;
                        }

                    case EventKind.ContractDeploy:
                        {
                            var name = evt.GetContent<string>();
                            sb.AppendLine($"Deployed contract: {name}</a>");
                            Nexus.RegisterSearch(name, "Deployment", SearchResultKind.Transaction, this.Hash.ToString());
                            break;
                        }

                    case EventKind.PlatformCreate:
                        {
                            var name = evt.GetContent<string>();
                            sb.AppendLine($"Created platform: {name}");
                            Nexus.RegisterSearch(name, "Platform Creation", SearchResultKind.Transaction, this.Hash.ToString());
                            break;
                        }

                    case EventKind.OrganizationCreate:
                        {
                            var name = evt.GetContent<string>();
                            sb.AppendLine($"Created organization: {name}");
                            Nexus.RegisterSearch(name, "Organization Creation", SearchResultKind.Transaction, this.Hash.ToString());
                            break;
                        }

                    case EventKind.ChainSwap:
                        {
                            var data = evt.GetContent<TransactionSettleEventData>();
                            sb.AppendLine($"Settled {data.Platform} transaction <a href=\"https://neoscan.io/transaction/{data.Hash}\">{data.Hash}</a>");
                            Nexus.RegisterSearch(data.Hash.ToString(), "Settlement", SearchResultKind.Transaction, this.Hash.ToString());
                            break;
                        }

                    case EventKind.ValidatorElect:
                        {
                            var address = evt.GetContent<Address>();
                            sb.AppendLine($"Elected validator: {LinkAddress(address)}");
                            break;
                        }

                    case EventKind.ValidatorPropose:
                        {
                            var address = evt.GetContent<Address>();
                            sb.AppendLine($"Proposed validator: {LinkAddress(address)}");
                            break;
                        }

                    case EventKind.ValidatorSwitch:
                        {
                            sb.AppendLine($"Switched validator: {LinkAddress(evt.Address)}");
                            break;
                        }

                    case EventKind.ValueCreate:
                        {
                            var data = evt.GetContent<ChainValueEventData>();
                            sb.AppendLine($"Created governance value: {data.Name}");
                            Nexus.RegisterSearch(data.Name, "Value Creation", SearchResultKind.Transaction, this.Hash.ToString());
                            break;
                        }

                    case EventKind.ValueUpdate:
                        {
                            var data = evt.GetContent<ChainValueEventData>();
                            sb.AppendLine($"Updated governance value: {data.Name}");
                            Nexus.RegisterSearch(data.Name, "Value Update", SearchResultKind.Transaction, this.Hash.ToString());
                            break;
                        }

                    case EventKind.GasEscrow:
                        {
                            var data = evt.GetContent<GasEventData>();
                            sb.AppendLine($"{LinkAddress(evt.Address)} escrowed {data.amount} fuel at price {UnitConversion.ToDecimal(data.price, DomainSettings.FuelTokenDecimals)} {DomainSettings.FuelTokenSymbol}");
                            break;
                        }

                    case EventKind.GasPayment:
                        {
                            var data = evt.GetContent<GasEventData>();
                            sb.AppendLine($"{LinkAddress(evt.Address)} paid {data.amount} fuel at price {UnitConversion.ToDecimal(data.price, DomainSettings.FuelTokenDecimals)} {DomainSettings.FuelTokenSymbol}");
                            break;
                        }

                    case EventKind.TokenMint:
                        {
                            var data = evt.GetContent<TokenEventData>();
                            var token = Nexus.FindTokenBySymbol(data.Symbol);
                            bool fungible = token.IsFungible();
                            if (fungible)
                            {
                                sb.AppendLine($"{LinkAddress(evt.Address)} minted {UnitConversion.ToDecimal(data.Value, token != null ? token.Decimals : 0)} {LinkToken(data.Symbol)}");
                            }
                            else
                            {
                                sb.AppendLine($"{LinkAddress(evt.Address)} minted {LinkToken(data.Symbol)} - NFT #{data.Value}");
                            }

                            Nexus.RegisterSearch(data.Symbol, "Token Mint", SearchResultKind.Transaction, this.Hash.ToString());
                            break;
                        }

                    case EventKind.TokenBurn:
                        {
                            var data = evt.GetContent<TokenEventData>();
                            var token = Nexus.FindTokenBySymbol(data.Symbol);
                            bool fungible = token.IsFungible();
                            if (fungible)
                            {
                                sb.AppendLine($"{LinkAddress(evt.Address)} burned {UnitConversion.ToDecimal(data.Value, token != null ? token.Decimals : 0)} {LinkToken(data.Symbol)}");
                            }
                            else
                            {
                                sb.AppendLine($"{LinkAddress(evt.Address)} burned {LinkToken(data.Symbol)} - NFT #{data.Value}");
                            }
                            Nexus.RegisterSearch(data.Symbol, "Token Burn", SearchResultKind.Transaction, this.Hash.ToString());
                            break;
                        }

                    case EventKind.TokenClaim:
                        {
                            var data = evt.GetContent<TokenEventData>();
                            var token = Nexus.FindTokenBySymbol(data.Symbol);
                            bool fungible = token.IsFungible();
                            if (fungible)
                            {
                                sb.AppendLine($"{LinkAddress(evt.Address)} claimed {UnitConversion.ToDecimal(data.Value, token != null ? token.Decimals : 0)} {LinkToken(data.Symbol)}");
                            }
                            else
                            {
                                sb.AppendLine($"{LinkAddress(evt.Address)} claimed {LinkToken(data.Symbol)} - NFT #{data.Value}");
                            }                          
                            break;
                        }

                    case EventKind.TokenStake:
                        {
                            var data = evt.GetContent<TokenEventData>();
                            var token = Nexus.FindTokenBySymbol(data.Symbol);

                            if (evt.Contract == "entry")
                            {
                                sb.AppendLine($"{LinkAddress(evt.Address)} staked {UnitConversion.ToDecimal(data.Value, token != null ? token.Decimals : 0)} {LinkToken(data.Symbol)}");
                            }
                            else
                            {
                                var contractAddress = Address.FromHash(evt.Contract);
                                bool fungible = token.IsFungible();
                                if (fungible)
                                {
                                    sb.AppendLine($"{LinkAddress(evt.Address)} deposited {UnitConversion.ToDecimal(data.Value, token != null ? token.Decimals : 0)} {LinkToken(data.Symbol)} into {LinkAddress(contractAddress, evt.Contract)} contract");
                                }
                                else
                                {
                                    sb.AppendLine($"{LinkAddress(evt.Address)} deposited {LinkToken(data.Symbol)} - NFT #{data.Value} into {LinkAddress(contractAddress, evt.Contract)} contract");
                                }
                            }
                            break;
                        }

                    case EventKind.TokenSend:
                        {
                            var data = evt.GetContent<TokenEventData>();
                            var token = Nexus.FindTokenBySymbol(data.Symbol);
                            bool fungible = token.IsFungible();
                            if (fungible)
                            {
                                sb.AppendLine($"{LinkAddress(evt.Address)} sent {UnitConversion.ToDecimal(data.Value, token != null ? token.Decimals : 0)} {LinkToken(data.Symbol)}");
                            }
                            else
                            {
                                sb.AppendLine($"{LinkAddress(evt.Address)} sent {LinkToken(data.Symbol)} - NFT #{data.Value}");
                            }
                            break;
                        }

                    case EventKind.TokenReceive:
                        {
                            var data = evt.GetContent<TokenEventData>();
                            var token = Nexus.FindTokenBySymbol(data.Symbol);
                            bool fungible = token.IsFungible();
                            if (fungible)
                            {
                                sb.AppendLine($"{LinkAddress(evt.Address)} received {UnitConversion.ToDecimal(data.Value, token != null ? token.Decimals : 0)} {LinkToken(data.Symbol)}");
                            }
                            else
                            {
                                sb.AppendLine($"{LinkAddress(evt.Address)} received {LinkToken(data.Symbol)} - NFT #{data.Value}");
                            }
                            break;
                        }
                }
            }

            if (!feeAddress.IsNull)
            {
                sb.AppendLine($"{LinkAddress(feeAddress)} paid {totalFees} {LinkToken(DomainSettings.FuelTokenSymbol)} in fees.");
            }

            foreach (var entry in fees)
            {
                sb.AppendLine($"{LinkAddress(entry.Key)} received {entry.Value} {LinkToken(DomainSettings.FuelTokenSymbol)} in fees.");
            }

            foreach (var addr in addresses)
            {
                var account = Nexus.FindAccount(addr, false);
                if (account != null)
                {
                    account.Transactions.Add(this.Hash);
                    Nexus.RegisterSearch(addr.Text, this.Hash.ToString(), SearchResultKind.Transaction, this.Hash.ToString());
                    //Nexus._addresses.Add(addr);
                }
            }

            if (sb.Length > 0)
            {
                return sb.ToString().Replace("\n","<br>");
            }

            return "Custom Transaction";
        }
    }

    public class ChainData : ExplorerObject, IChain
    {
        public ChainData(NexusData Database, DataNode node) : base(Database)
        {
            this.Name = node.GetString("name");
            this.Address = Address.FromText(node.GetString("address"));
            this.Height = BigInteger.Parse(node.GetString("height"));
            this.BlockList = new List<BlockData>((int)Height);

            var contractsNodes = node.GetNode("contracts");
            if (contractsNodes != null)
            {
                Contracts = new Address[contractsNodes.ChildCount];
                for (int i=0; i<Contracts.Length; i++)
                {
                    var name = contractsNodes.GetString(i);
                    Contracts[i] = Address.FromHash(name);
                    Nexus.RegisterSearch(name, name, SearchResultKind.Address, Contracts[i].Text);
                }
            }
            else
            {
                Contracts = new Address[0];
            }
        }

        public void LoadFromCache()
        {
            if (string.IsNullOrEmpty(Nexus.cachePath))
            {
                return;
            }

            for (int i = 0; i < Height; i++)
            {
                var fileName = Nexus.GetBlockCacheFileName(this, i + 1);
                if (File.Exists(fileName))
                {
                    var xml = File.ReadAllText(fileName);
                    var temp = XMLReader.ReadFromString(xml);
                    temp = temp.GetNodeByIndex(0);
                    var block = new BlockData(Nexus, temp);

                    BlockList.Add(null);
                    RegisterBlock(i, block);

                    Nexus.AcknowledgeBlock(block);

                    Console.WriteLine($"Loaded block from cache: {this.Name}, {i+1} out of {Height}");
                }
                else
                {
                    break;
                }           
            }
        }

        public void Grow(int height)
        {
            if (height < 0)
            {
                throw new Exception("Invalid chain height: " + height);
            }

            this.Height = height;
            //UpdateBlocks();
        }

        internal void UpdateBlocks()
        {
            int currentHeight = BlockList.Count;

            int newBlocks = 0;
            while (BlockList.Count < Height)
            {
                BlockList.Add(null);
                newBlocks++;
            }

            if (newBlocks <= 0)
            {
                return;
            }

            Nexus.DoParallelRequests($"Fetching new blocks for chain {Name}...", newBlocks,  true, (index) =>
            {
                var ofs = index + currentHeight;
                var block = Nexus.FindBlockByHeight(this, ofs + 1);
                RegisterBlock(ofs, block);
            });
        }

        private void RegisterBlock(int ofs, BlockData block)
        {
            BlockList[ofs] = block;

            if (ofs == 0 && this.Name == DomainSettings.RootChainName)
            {
                Nexus.RegisterSearch("genesis", null, SearchResultKind.Address, block.ValidatorAddress.Text);
            }
            else
            {
                block.RegisterBlockContents();
            }
        }

        public string Name { get; private set; }
        public Address Address { get; private set; }
        public BigInteger Height { get; private set; }

        public ChainData ParentChain { get; private set; }
        public ChainData[] ChildChains { get; private set; }

        public Address[] Contracts { get; private set; }

        public List<BlockData> BlockList { get; private set; }

        public BlockData LastBlock => BlockList[BlockList.Count - 1];

        public IEnumerable<BlockData> Blocks => BlockList.Skip(Math.Max(0, (int)(Height - 20))).Reverse();
    }

    public class ContractData : ExplorerObject, IContract
    {
        public string Name { get; private set; }

        public ContractInterface ABI => throw new NotImplementedException();

        public ContractData(NexusData database, DataNode node): base(database)
        {

        }
    }

    public class TokenData : ExplorerObject, IToken
    {
        public TokenData(NexusData database, DataNode node) : base(database)
        {
            this.Name = node.GetString("name");
            this.Symbol = node.GetString("symbol");
            this.MaxSupply = BigInteger.Parse(node.GetString("maxSupply"));
            this.CurrentSupply = BigInteger.Parse(node.GetString("currentSupply"));
            this.Decimals = int.Parse(node.GetString("decimals"));
            this.Flags = node.GetEnum<TokenFlags>("flags");
            this.Script = null; // TODO
        }

        public string Name { get; private set; }
        public string Symbol { get; private set; }
        public TokenFlags Flags { get; private set; }
        public BigInteger MaxSupply { get; private set; }
        public BigInteger CurrentSupply { get; private set; }
        public int Decimals { get; private set; }
        public byte[] Script { get; private set; }

        public decimal FormattedMaxSupply => UnitConversion.ToDecimal(MaxSupply, Decimals);
        public decimal FormattedCurrentSupply => UnitConversion.ToDecimal(CurrentSupply, Decimals);

        public decimal Price = 0;

        public bool IsFungible => this.Flags.HasFlag(TokenFlags.Fungible);
        public bool IsFiat => this.Flags.HasFlag(TokenFlags.Fiat);
        public bool IsFinite => this.Flags.HasFlag(TokenFlags.Finite);
        public bool IsTransferable => this.Flags.HasFlag(TokenFlags.Transferable);
    }

    public class PlatformData : ExplorerObject, IPlatform
    {
        public PlatformData(NexusData database, DataNode node) :base(database)
        {
            Name = node.GetString("platform");
            Symbol = node.GetString("fuel");
            InteropAddresses = null;// platform.interop.Select(x => new PlatformSwapAddress() { ExternalAddress = x.external, LocalAddress = Address.FromText(x.local) }).ToArray();
        }

        public string Name { get; private set; }
        public string Symbol { get; private set; }
        public PlatformSwapAddress[] InteropAddresses { get; private set; }
    }

    public class BalanceData: ExplorerObject
    {
        public ChainData Chain { get; private set; }
        public BigInteger Amount { get; private set; }
        public string Symbol { get; private set; }
        public BigInteger[] IDs { get; private set; }

        private int decimals;

        public decimal FormattedAmount => UnitConversion.ToDecimal(Amount, decimals);

        public BalanceData(NexusData database, DataNode node) : base(database)
        {
            var chainName = node.GetString("chain");
            Chain = database.FindChainByName(chainName);
            Symbol = node.GetString("symbol");
            decimals = node.GetInt32("decimals");
            Amount = BigInteger.Parse(node.GetString("amount"));

            var idNode = node.GetNode("ids");
            if (idNode != null)
            {
                IDs = new BigInteger[idNode.ChildCount];
                for (int i = 0; i < IDs.Length; i++)
                {
                    IDs[i] = BigInteger.Parse(idNode.GetString(i));
                }
            }
            else
            {
                IDs = null;
            }
        }
    }

    public class LeaderboardData: ExplorerObject
    {
        public string Name { get; private set; }
        public LeaderboardEntry[] Rows { get; private set; }

        public LeaderboardData(NexusData database, DataNode node) : base(database)
        {
            this.Name = node.GetString("name");

            var rowsNode = node.GetNode("rows");
            this.Rows = new LeaderboardEntry[rowsNode.ChildCount];
            for (int i=0; i<Rows.Length; i++)
            {
                var temp = rowsNode.GetNodeByIndex(i);
                var addr = Address.FromText(temp.GetString("address"));
                var score = temp.GetInt32("value");

                this.Rows[i] = new LeaderboardEntry()
                {
                    formatted = "?",
                    ranking = i+1,
                    address = addr,
                    score = score
                };
            }
        }
    }

    public class AccountData : ExplorerObject
    {
        public AccountData(NexusData database, DataNode node) : base(database)
        {
            this.Transactions = new HashSet<Hash>();

            Name = node.GetString("name");
            Address = Cryptography.Address.FromText(node.GetString("address"));

            var stakeNode = node.GetNode("stakes");
            Stake = BigInteger.Parse(stakeNode.GetString("amount"));

            var balancesNode = node.GetNode("balances");
            if (balancesNode != null)
            {
                Balances = new BalanceData[balancesNode.ChildCount];
                for (int i = 0; i < Balances.Length; i++)
                {
                    var temp = balancesNode.GetNodeByIndex(i);
                    Balances[i] = new BalanceData(database, temp);
                }
            }
            else
            {
                Balances = new BalanceData[0];
            }

            var txsNode = node.GetNode("txs");
            if (txsNode != null)
            {
                for (int i = txsNode.ChildCount - 1; i >= 0 ; i--)
                {
                    var hash = Hash.Parse(txsNode.GetString(i));
                    Transactions.Add(hash);
                }
            }

            LastTime = DateTime.UtcNow;
        }

        internal DateTime LastTime { get; private set; }

        public string Name { get; private set; }
        public Address Address { get; private set; }

        public BigInteger Stake { get; private set; }
        public decimal FormattedStake => UnitConversion.ToDecimal(Stake, DomainSettings.StakingTokenDecimals);

        public BalanceData[] Balances { get; private set; }

        public HashSet<Hash> Transactions { get; internal set; }

        public bool IsEmpty
        {
            get
            {
                if (Address.Kind != AddressKind.User)
                {
                    return false;
                }

                return Name==ValidationUtils.ANONYMOUS && Stake == 0 && Balances.Length == 0;
            }
        }
    }

    public class GovernanceData: ExplorerObject
    {
        public GovernanceData(NexusData database, DataNode node): base(database)
        {
            Name = node.GetString("name");
            Value = node.GetString("value");
        }

        public string Name { get; private set; }
        public string Value { get; private set; }
    }

    public class OrganizationData : ExplorerObject
    {
        public OrganizationData(NexusData database, DataNode node) : base(database)
        {
            ID = node.GetString("id");
            Name = node.GetString("name");
            var membersNode = node.GetNode("members");
            Members = new Address[membersNode.ChildCount];
            for (int i=0; i<Members.Length; i++)
            {
                Members[i] = Address.FromText( membersNode.GetString(i));
            }

            this.Address = Address.FromHash(ID);
        }

        public void UpdateAccounts()
        {
            this.Nexus.DoParallelRequests($"Fetching accounts for {ID}...", Members.Length, true, (index) =>
            {
                Nexus.FindAccount(Members[index], false);
            });
        }

        public string ID { get; private set; }
        public string Name { get; private set; }
        public Address[] Members { get; private set; }
        public Address Address { get; private set; }
        public int Size => Members.Length;
    }

    public struct LeaderboardEntry
    {
        public Address address;
        public BigInteger score;
        public int ranking;
        public string formatted;
    }

    public class NexusData
    {
        public string UpdateStatus { get; private set; }
        public int UpdateProgress { get; private set; }
        public string Name { get; private set; }

        private Dictionary<string, ChainData> _chains = new Dictionary<string, ChainData>();
        private Dictionary<string, TokenData> _tokens = new Dictionary<string, TokenData>();
        private Dictionary<string, PlatformData> _platforms = new Dictionary<string, PlatformData>();
        public Dictionary<string, OrganizationData> _organizations = new Dictionary<string, OrganizationData>();

        private Dictionary<Hash, BlockData> _blocks = new Dictionary<Hash, BlockData>();
        private Dictionary<Hash, TransactionData> _transactions = new Dictionary<Hash, TransactionData>();

        private Dictionary<Address, AccountData> _accounts = new Dictionary<Address, AccountData>();

        private Dictionary<string, LeaderboardData> _leaderboards = new Dictionary<string, LeaderboardData>();

        private List<GovernanceData> _governance = new List<GovernanceData>();

        /*internal HashSet<Address> _addresses = new HashSet<Address>();
        public IEnumerable<Address> Addresses => _addresses;
        */

        private string RESTurl;

        private Queue<TransactionData> _transactionQueue = new Queue<TransactionData>();

        public IEnumerable<ChainData> Chains => _chains.Values;
        public IEnumerable<TokenData> Tokens => _tokens.Values;
        public IEnumerable<PlatformData> Platforms => _platforms.Values;
        public IEnumerable<OrganizationData> Organizations => _organizations.Values;
        public IEnumerable<GovernanceData> Governance => _governance;
        public ChainData RootChain => FindChainByName("main");

        private int updateCount;

        public readonly string cachePath;

        public NexusData(string RESTurl, string cachePath)
        {
            if (!cachePath.EndsWith("/"))
            {
                cachePath += "/";
            }
            this.cachePath = cachePath;

            if (!Directory.Exists(cachePath))
            {
                Directory.CreateDirectory(cachePath);
            }

            if (!RESTurl.EndsWith("/"))
            {
                RESTurl += "/";
            }
            this.RESTurl = RESTurl;
        }

        public bool Update()
        {
            this.UpdateStatus = "Connecting to nexus";
            this.UpdateProgress = 0;

            var node = APIRequest("getNexus");
            if (node == null)
            {
                return false;
            }

            bool generateDescriptions = false;

            this.Name = node.GetString("name");

            this.UpdateStatus = "Fetching tokens";
            this.UpdateProgress = 0;

            var tokens = node.GetNode("tokens");
            foreach (var entry in tokens.Children)
            {
                var token = new TokenData(this, entry);
                if (!_tokens.ContainsKey(token.Symbol))
                {
                    Console.WriteLine("Detected new token: " + token.Name);
                    _tokens[token.Symbol] = token;

                    RegisterSearch(token.Symbol, token.Name, SearchResultKind.Token);
                    RegisterSearch(token.Name, null, SearchResultKind.Token, token.Symbol);
                }
            }

            this.UpdateStatus = "Fetching chains";
            this.UpdateProgress = 0;

            var chains = node.GetNode("chains");
            foreach (var entry in chains.Children)
            {
                var chain = new ChainData(this, entry);

                if (_chains.ContainsKey(chain.Name))
                {
                    var height = chain.Height;
                    chain = _chains[chain.Name];
                    var diff = height - chain.Height;
                    if (diff > 0)
                    {
                        Console.WriteLine($"Detected {diff} new blocks on chain {chain.Name}");
                        chain.Grow((int)height);
                        generateDescriptions = true;
                    }
                }
                else
                {
                    Console.WriteLine("Detected new chain: " + chain.Name);
                    _chains[chain.Name] = chain;

                    RegisterSearch(chain.Name, null, SearchResultKind.Chain);
                    generateDescriptions = true;

                    chain.LoadFromCache();
                }
            }

            this.UpdateStatus = "Fetching platforms";
            this.UpdateProgress = 0;

            var platforms = node.GetNode("platforms");
            if (platforms != null)
            {
                foreach (var entry in platforms.Children)
                {
                    var platform = new PlatformData(this, entry);
                    _platforms[platform.Name] = platform;

                    RegisterSearch(platform.Name, null, SearchResultKind.Platform);
                }
            }

            this.UpdateStatus = "Fetching governance";
            this.UpdateProgress = 0;

            var govNode = node.GetNode("governance");
            foreach (var entry in govNode.Children)
            {
                var gov = new GovernanceData(this, entry);
                _governance.Add(gov);
            }

            this.UpdateStatus = "Fetching organizations";
            this.UpdateProgress = 0;

            var orgNode = node.GetNode("organizations");
            if (orgNode != null)
            {
                foreach (var entry in orgNode.Children)
                {
                    var name = entry.Value;
                    FindOrganization(name);
                }
            }

            this.UpdateStatus = "Fetching master accounts";
            this.UpdateProgress = 0;
            if (updateCount == 0)
            {
                var masters = _organizations["masters"];
                masters.UpdateAccounts();

                /*new Thread(() =>
                {
                    Console.WriteLine($"Updating {_transactions.Count} transactions...");
                    foreach (var tx in _transactions.Values)
                    {
                        var temp = tx.Description;
                    }
                }).Start();*/
            }
            else
            {
                var masters = UpdateOrganization("masters");
                if (masters != null)
                {
                    masters.UpdateAccounts();
                }
            }

            Console.WriteLine($"Updating {_chains.Count} chains...");
            foreach (var chain in _chains.Values)
            {
                this.UpdateStatus = "Fetching blocks for chain "+chain.Name;
                this.UpdateProgress = 0;

                chain.UpdateBlocks();
            }

            updateCount++;

            if (generateDescriptions)
            {
                this.UpdateStatus = "Generating transaction descriptions";
                this.UpdateProgress = 0;

                Queue<TransactionData> queue;
                lock (_transactionQueue)
                {
                    queue = _transactionQueue;
                    _transactionQueue = new Queue<TransactionData>();
                }

                new Thread(() =>
                {
                    var total = queue.Count;
                    int current = 0;

                    while (queue.Count > 0)
                    {
                        var tx = queue.Dequeue();
                        var temp = tx.Description;
                        current++;

                        this.UpdateProgress = (current * 100)/total;
                    }

                    Console.Write($"Finished generating {total} tx descriptions");
                }).Start();
            }

            return true;
        }

        public DataNode APIRequest(string path)
        {
            var url = RESTurl + path;
            //Console.WriteLine("Request: " + url);

            int max = 5;
            for (int i=1; i<=max; i++)
            {
                try
                {
                    string contents;
                    using (var wc = new System.Net.WebClient())
                    {
                        contents = wc.DownloadString(url);
                    }

                    var node = LunarLabs.Parser.JSON.JSONReader.ReadFromString(contents);
                    return node;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    if (i < max)
                    {
                        Thread.Sleep(1000*i);
                        Console.WriteLine("Trying again...");
                    }
                }
            }

            return null;
        }

        public ChainData FindChainByName(string name)
        {
            if (_chains.ContainsKey(name))
            {
                return _chains[name];
            }

            return null;
        }

        internal ChainData FindChainByAddress(Address chainAddress)
        {
            foreach (var chain in _chains.Values)
            {
                if (chain.Address == chainAddress)
                {
                    return chain;
                }
            }

            return null;
        }

        internal TokenData FindTokenBySymbol(string symbol)
        {
            if (_tokens.ContainsKey(symbol))
            {
                return _tokens[symbol];
            }

            return null;
        }

        public decimal ToDecimal(BigInteger amount, string symbol)
        {
            if (_tokens.ContainsKey(symbol))
            {
                var token = _tokens[symbol];
                return UnitConversion.ToDecimal(amount, token.Decimals);
            }

            return 0;
        }

        public OrganizationData FindOrganization(string id)
        {
            if (_organizations.ContainsKey(id))
            {
                return _organizations[id];
            }

            Console.WriteLine("Detected new organization: " + id);
            var temp = APIRequest("getOrganization/" + id);
            var org = new OrganizationData(this, temp);
            _organizations[id] = org;

            RegisterSearch(id, org.Name, SearchResultKind.Organization, id);
            RegisterSearch(org.Name, null, SearchResultKind.Organization, id);

            return org;
        }

        public OrganizationData UpdateOrganization(string id)
        {
            if (_organizations.ContainsKey(id))
            {
                Console.WriteLine("Updating organization: " + id);
                var temp = APIRequest("getOrganization/" + id);
                var org = new OrganizationData(this, temp);
                _organizations[id] = org;

                return _organizations[id];
            }

            return null;
        }

        public TransactionData FindTransaction(ChainData chain, Hash txHash)
        {
            if (_transactions.ContainsKey(txHash))
            {
                return _transactions[txHash];
            }

            var node = APIRequest($"getTransaction?hashText={txHash}");
            if (node != null && !String.IsNullOrEmpty(node.GetString("chainAddress")))
            {
                var tx = new TransactionData(this, node);
                lock (_transactions)
                {
                    _transactions[tx.Hash] = tx;
                }

                lock (_transactionQueue)
                {
                    _transactionQueue.Enqueue(tx);
                }

                var fileName = GetTransactionCacheFileName(txHash);
                if (fileName != null && !File.Exists(fileName))
                {
                    var xml = XMLWriter.WriteToString(node);
                    File.WriteAllText(fileName, xml);
                }

                RegisterSearch(txHash.ToString(), null, SearchResultKind.Transaction);
                return tx;
            }
            else
            {
                return null;
            }
        }
        public string GetBlockCacheFileName(ChainData chain, int height)
        {
            return GetCacheFileName($"{chain.Name}_block_{height}");
        }
        public string GetTransactionCacheFileName(Hash hash)
        {
            return GetCacheFileName($"tx_{hash}");
        }

        public string GetCacheFileName(string desc)
        {
            if (string.IsNullOrEmpty(cachePath))
            {
                return null;
            }

            var md5Hasher = MD5.Create();
            var hashed = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(desc));
            var bucket = BitConverter.ToUInt32(hashed, 0);
            bucket %= 1024;

            var dir = $"{cachePath}/{bucket}";
            if (!Directory.Exists(dir))
            {
                Console.WriteLine("Creating cache dir: " + dir);
                Directory.CreateDirectory(dir);
            }

            return $"{dir}/{desc}.xml";
        }

        internal void AcknowledgeBlock(BlockData block)
        {
            lock (_blocks)
            {
                _blocks[block.Hash] = block;
                RegisterSearch(block.Height.ToString(), block.Hash.ToString(), SearchResultKind.Block, block.Hash.ToString());
            }
        }

        internal BlockData FindBlockByHeight(ChainData chain, int height)
        {
            var node = APIRequest($"getBlockByHeight?chainInput={chain.Name}&height={height}");
            var block = new BlockData(this, node);

            AcknowledgeBlock(block);

            var fileName = GetBlockCacheFileName(chain, height);
            if (fileName != null && !File.Exists(fileName))
            {
                var xml = XMLWriter.WriteToString(node);
                File.WriteAllText(fileName, xml);
            }

            return block;
        }

        internal BlockData FindBlockByHash(ChainData chainData, Hash hash)
        {
            lock (_blocks)
            {
                if (_blocks.ContainsKey(hash))
                {
                    return _blocks[hash];
                }
            }

            return null;
        }

        public LeaderboardData FindLeaderboard(string name)
        {
            lock (_leaderboards)
            {
                if (_leaderboards.ContainsKey(name))
                {
                    return _leaderboards[name];
                }
            }

            var node = APIRequest("getLeaderboard/" + name);

            if (node != null)
            {
                var leaderboard = new LeaderboardData(this, node);

                lock (_leaderboards)
                {
                    _leaderboards[name] = leaderboard;
                }

                RegisterSearch(name, null, SearchResultKind.Leaderboard);
                return leaderboard;
            }

            return null;
        }

        public AccountData FindAccount(Address address, bool canExpire)
        {
            AccountData account = null;

            lock (_accounts)
            {
                if (_accounts.ContainsKey(address))
                {
                    account = _accounts[address];

                    if (account != null)
                    {
                        var diff = DateTime.UtcNow - account.LastTime;
                        if (!canExpire || diff.TotalSeconds < 60)
                        {
                            return account;
                        }
                    }
                }
            }

            var node = APIRequest("getAccount/" + address.Text);

            if (node != null)
            {
                var prev = account;

                account = new AccountData(this, node);
                if (account.IsEmpty)
                {
                    return null;
                }

                if (prev != null)
                {
                    account.Transactions = prev.Transactions;
                }

                lock (_accounts)
                {
                    _accounts[address] = account;
                }

                RegisterSearch(account.Address.Text, account.Name, SearchResultKind.Address);
                if(!String.IsNullOrEmpty(account.Name))
                {
                    // We need this for search by account name.
                    RegisterSearch(account.Name, null, SearchResultKind.Address, account.Address.Text);
                }

                return account;
            }

            return null;
        }

        public void DoParallelRequests(string description, int total, bool isStatus, Action<int> fetcher)
        {
            if (total <= 0)
            {
                return;
            }

            Console.WriteLine(description);

            //var progress = new ProgressBar();

            var blockSize = 16;

            //int finished = 0;

            if (isStatus)
            {
                this.UpdateStatus = description;
                this.UpdateProgress = 0;
            }

            int max = total;

            int offset = 0;
            while (total > 0)
            {
                var roundSize = Math.Min(blockSize, total);

                var tasks = new Task[roundSize];

                for (int i = 0; i < roundSize; i++)
                {
                    var index = i + offset;
                    tasks[i] = new Task(() =>
                    {
                        fetcher(index);
                        /*lock (progress)
                        {
                            finished++;
                            progress.Report(finished / (float)total);
                        }*/
                    });
                }

                foreach (var task in tasks)
                {
                    task.Start();
                }

                Task.WaitAll(tasks);

                offset += roundSize;
                total -= roundSize;

                if (isStatus)
                {
                    this.UpdateProgress = (offset * 100) / max;
                }
            }
        }

        internal Event[] ReadEvents(DataNode node)
        {
            var eventsNode = node.GetNode("events");
            if (eventsNode == null)
            {
                return new Event[0];
            }

            var events = new Event[eventsNode.ChildCount];
            for (int i = 0; i < events.Length; i++)
            {
                var temp = eventsNode.GetNodeByIndex(i);

                var kind = temp.GetEnum<EventKind>("kind");
                var contract = temp.GetString("contract");
                var addr = Address.FromText(temp.GetString("address"));
                var data = Base16.Decode(temp.GetString("data"));

                events[i] = new Event(kind, addr, contract, data);
            }

            return events;
        }

        private Dictionary<string, List<SearchResult>> _search = new Dictionary<string, List<SearchResult>>(StringComparer.OrdinalIgnoreCase);

        public void RegisterSearch(string key, string text, SearchResultKind kind, string data = null)
        {
            if (data == null)
            {
                data = key;
            }

            if (text == null)
            {
                text = key;
            }

            var item = new SearchResult()
            {
                Kind = kind,
                Text = text,
                Data = data,
            };

            List<SearchResult> results;

            lock (_search)
            {
                if (_search.ContainsKey(key))
                {
                    results = _search[key];
                }
                else
                {
                    results = new List<SearchResult>();
                    _search[key] = results;
                }

                foreach (var other in results)
                {
                    if (other.Kind == item.Kind && other.Data == item.Data)
                    {
                        return;
                    }
                }

                results.Add(item);
            }

            if (text != data && text.Contains(" "))
            {
                var firstName = text.Split(' ')[0];
                RegisterSearch(firstName, text, kind, data);
            }
        }

        public IEnumerable<SearchResult> SearchItem(string text)
        {
            if (_search.ContainsKey(text))
            {
                return _search[text];
            }

            return Enumerable.Empty<SearchResult>();
        }
    }
}
