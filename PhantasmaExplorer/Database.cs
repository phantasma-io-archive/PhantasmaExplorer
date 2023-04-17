using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using LunarLabs.Parser;
using LunarLabs.Parser.XML;

using Phantasma.Explorer.Utils;
using Phantasma.Core.Types;
using Phantasma.Core.Cryptography;
using Phantasma.Core.Domain;
using Phantasma.Core.Numerics;
using Phantasma.Core;
using Phantasma.Business.Blockchain;
using Phantasma.Business.Blockchain.Contracts;
using Phantasma.Business.Blockchain.VM;
using System.Numerics;
using Phantasma.Business.VM;
using Phantasma.Business.Blockchain.Contracts.Native;
//using Phantasma.Pay.Chains;

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
        File,
        Sale,
    }

    public struct SearchResult
    {
        public SearchResultKind Kind;
        public string Text;
        public string Data;
    }

    public class CustomDescriptionVM : DescriptionVM
    {
        public NexusData Nexus;

        public CustomDescriptionVM(NexusData nexus, byte[] script) : base(script, 0)
        {
            this.Nexus = nexus;
        }

        public override IToken FetchToken(string symbol)
        {
            var token = this.Nexus.FindTokenBySymbol(symbol);
            if (token == null)
            {
                throw new Exception("unknown token: " + symbol);
            }

            return token;
        }

        public override string OutputAddress(Address address)
        {
            return LinkAddressTag.GenerateLink(this.Nexus, address);
        }

        public override string OutputSymbol(string symbol)
        {
            return $"<a href=\"/token/{symbol}\">{symbol}</a>";
        }
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
        public uint TxsCount { get; private set; }
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
            TxsCount = (uint)TransactionHashes.Length;
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
                        lock (Transactions)
                        {
                            Transactions[i] = tx;
                        }
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

                case EventKind.ContractUpgrade:
                        {
                            var symbol = evnt.GetContent<string>();
                            Nexus.RegisterSearch(symbol, "Contract Upgrade", SearchResultKind.Transaction, hash.ToString());
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

                case EventKind.Crowdsale:
                    {
                        var data = evnt.GetContent<SaleEventData>();
                        var sale = Nexus.FindSaleByHash(data.saleHash);
                        if (sale != null)
                        {
                            switch (data.kind)
                            {
                                case SaleEventKind.Creation:
                                    Nexus.RegisterSearch(sale.Name, "Sale creation", SearchResultKind.Sale, hash.ToString());
                                    break;
                            }
                        }
                        break;
                    }

            }
        }
    }

    public class TransactionData : ExplorerObject//, ITransaction
    {
        public TransactionData(NexusData database, DataNode node) : base(database)
        {
            Script = Base16.Decode(node.GetString("script"));
            NexusName = Nexus.Name;
            ChainAddress = Address.FromText(node.GetString("chainAddress"));
            BlockHash = Hash.Parse(node.GetString("blockHash"));
            Expiration = new Timestamp(node.GetUInt32("expiration"));
            Payload = Base16.Decode(node.GetString("payload"));
            Signatures = null; // TODO
            Hash = Hash.Parse(node.GetString("hash"));

            Timestamp = new Timestamp(node.GetUInt32("timestamp"));

            bool isStringPayload = true;
            foreach (var b in Payload)
            {
                if (b > 127)
                {
                    isStringPayload = false;
                    break;
                }
            }

            if (isStringPayload)
            {
                DecodedPayload = DecodedPayload = System.Text.Encoding.UTF8.GetString(Payload);
            }
            else
            {
                DecodedPayload = Base16.Encode(Payload);
            }

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
        private string _fees = null;

        public string Description
        {
            get
            {
                if (_description == null)
                {
                    GenerateDescription();
                }

                return _description;
            }
        }

        public string Fees
        {
            get
            {
                if (_fees == null)
                {
                    GenerateDescription();
                }

                return _fees;
            }
        }

        public string DecodedPayload { get; private set; }

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

        private string LinkSale(SaleData sale)
        {
            return $"<a href=\"/sale/{sale.Hash}\">{sale.Name}</a>";
        }

        private string LinkFile(Hash hash)
        {
            var hashText = hash.ToString();
            var file = this.Nexus.FindFile(hashText);
            var result = $"<a href=\"/file/{hashText}\">{file.Name}</a>";
            return result;
        }

        private void GenerateDescription()
        {
            decimal totalFees = 0;
            decimal burnedFees = 0;
            decimal crownFees = 0;
            var fees = new Dictionary<Address, decimal>();

            Address feeAddress = Address.Null;

            var addresses = new HashSet<Address>();

            bool skipEvent = false;

            var sb = new StringBuilder();
            for (int k=0; k<Events.Length; k++)
            {
                var evt = Events[k];

                if (evt.Address.IsUser)
                {
                    addresses.Add(evt.Address);                
                }

                if (skipEvent)
                {
                    skipEvent = false;
                    continue;
                }


                if (evt.Contract == "gas") // handle tokenmint & tokenclaim on gas contract (ex: crown)
                {
                    switch (evt.Kind)
                    {
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
                            break;
                        }

                        case EventKind.TokenClaim:
                        {
                            var data = evt.GetContent<TokenEventData>();
                            var token = Nexus.FindTokenBySymbol(data.Symbol);
                            bool fungible = token.IsFungible();

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
                            else
                            if (!fungible)
                            {
                                sb.AppendLine($"{LinkAddress(evt.Address)} claimed {LinkToken(data.Symbol)} - NFT #{data.Value}");
                            }

                            break;
                        }

                        case EventKind.Infusion:
                        {
                            var data = evt.GetContent<InfusionEventData>();
                            var token = Nexus.FindTokenBySymbol(data.BaseSymbol);
                            var tokenInfused = Nexus.FindTokenBySymbol(data.InfusedSymbol);
                            bool fungible = tokenInfused.IsFungible();

                            if (fungible)
                            {
                                sb.AppendLine($"{LinkAddress(evt.Address)} infused {LinkToken(token.Symbol)} - NFT #{data.TokenID} with {UnitConversion.ToDecimal(data.InfusedValue, tokenInfused != null ? tokenInfused.Decimals : 0)} {LinkToken(data.InfusedSymbol)}");
                            }
                            else if (token.Symbol == "TTRS")
                            {
                                if (data.InfusedSymbol == "TTRS")
                                {
                                    sb.AppendLine($"{LinkAddress(evt.Address)} infused {LinkToken(token.Symbol)} - NFT <a href=\"https://www.22series.com/part_info?id={data.TokenID}\" target=\"_blank\">#{data.TokenID}</a> with {LinkToken(data.InfusedSymbol)} - NFT <a href=\"https://www.22series.com/part_info?id={data.InfusedValue}\" target=\"_blank\">#{data.InfusedValue}</a>");
                                }
                                else if (data.InfusedSymbol == "GHOST")
                                {
                                    sb.AppendLine($"{LinkAddress(evt.Address)} infused {LinkToken(token.Symbol)} - NFT <a href=\"https://www.22series.com/part_info?id={data.TokenID}\" target=\"_blank\">#{data.TokenID}</a> with {LinkToken(data.InfusedSymbol)} - NFT <a href=\"https://ghostmarket.io/asset/pha/ttrs/{data.InfusedValue}\" target=\"_blank\">#{data.InfusedValue}</a>");
                                }
                                else
                                {
                                    sb.AppendLine($"{LinkAddress(evt.Address)} infused {LinkToken(token.Symbol)} - NFT #{data.TokenID} with {LinkToken(data.InfusedSymbol)} NFT #{data.InfusedValue}");
                                }
                            }
                            else if (token.Symbol == "GHOST")
                            {
                                if (data.InfusedSymbol == "TTRS")
                                {
                                    sb.AppendLine($"{LinkAddress(evt.Address)} infused {LinkToken(token.Symbol)} - NFT <a href=\"https://ghostmarket.io/asset/pha/ghost/{data.TokenID}\" target=\"_blank\">#{data.TokenID}</a> with {LinkToken(data.InfusedSymbol)} - NFT <a href=\"https://www.22series.com/part_info?id={data.InfusedValue}\" target=\"_blank\">#{data.InfusedValue}</a>");
                                }
                                else if (data.InfusedSymbol == "GHOST")
                                {
                                    sb.AppendLine($"{LinkAddress(evt.Address)} infused {LinkToken(token.Symbol)} - NFT <a href=\"https://ghostmarket.io/asset/pha/ghost/{data.TokenID}\" target=\"_blank\">#{data.TokenID}</a> with {LinkToken(data.InfusedSymbol)} - NFT <a href=\"https://ghostmarket.io/asset/pha/ghost/{data.InfusedValue}\" target=\"_blank\">#{data.InfusedValue}</a>");
                                }
                                else
                                {
                                    sb.AppendLine($"{LinkAddress(evt.Address)} infused {LinkToken(token.Symbol)} - NFT #{data.TokenID} with {LinkToken(data.InfusedSymbol)} NFT #{data.InfusedValue}");
                                }
                            }
                            else
                            {
                                sb.AppendLine($"{LinkAddress(evt.Address)} infused {LinkToken(token.Symbol)} - NFT #{data.TokenID} with {LinkToken(data.InfusedSymbol)} NFT #{data.InfusedValue}");
                            }
                            break;
                        }

                        case EventKind.TokenStake:
                            {
                                var data = evt.GetContent<TokenEventData>();
                                if (data.Symbol == DomainSettings.FuelTokenSymbol)
                                {
                                    feeAddress = evt.Address;
                                }
                                break;
                            }

                          case EventKind.TokenBurn:
                              {
                                  var data = evt.GetContent<TokenEventData>();
                                  if (data.Symbol == DomainSettings.FuelTokenSymbol)
                                  {
                                      if (evt.Address.IsSystem)
                                      {
                                          var amount = UnitConversion.ToDecimal(data.Value, DomainSettings.FuelTokenDecimals);
                                          burnedFees += amount;
                                      }
                                  }
                                  break;
                              }

                          case EventKind.CrownRewards:
                              {
                                  var data = evt.GetContent<TokenEventData>();
                                  if (data.Symbol == DomainSettings.FuelTokenSymbol)
                                  {
                                        var amount = UnitConversion.ToDecimal(data.Value, DomainSettings.FuelTokenDecimals);
                                        crownFees += amount;
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

                            string extra;

                            if (name != DomainSettings.RootChainName)
                            {
                                extra = "side-";
                            }
                            else
                            {
                                extra = "";
                            }

                            sb.AppendLine($"{LinkAddress(evt.Address)} created {extra}chain: <a href=\"/chain/{name}\">{name}</a>");
                            Nexus.RegisterSearch(name, "Chain Creation", SearchResultKind.Transaction, this.Hash.ToString());
                            break;
                        }

                    case EventKind.TokenCreate:
                        {
                            var symbol = evt.GetContent<string>();
                            sb.AppendLine($"{LinkAddress(evt.Address)} created token: {LinkToken(symbol)}");
                            Nexus.RegisterSearch(symbol, "Token Creation", SearchResultKind.Transaction, this.Hash.ToString());
                            break;
                        }

                    case EventKind.ContractUpgrade:
                            {
                                var symbol = evt.GetContent<string>();
                                sb.AppendLine($"{LinkAddress(evt.Address)} upgraded contract: {LinkToken(symbol)}");
                                Nexus.RegisterSearch(symbol, "Contract Upgrade", SearchResultKind.Transaction, this.Hash.ToString());
                                break;
                            }

                    case EventKind.AddressRegister:
                        {
                            var name = evt.GetContent<string>();
                            sb.AppendLine($"{LinkAddress(evt.Address, null)} registered name: {name}");
                            Nexus.RegisterSearch(name, "Name Registration", SearchResultKind.Transaction, this.Hash.ToString());
                            break;
                        }

                    case EventKind.ContractDeploy:
                        {
                            var name = evt.GetContent<string>();
                            sb.AppendLine($"{LinkAddress(evt.Address)} deployed contract: <a href=\"/contract/{name}/\">{name}</a>");
                            Nexus.RegisterSearch(name, "Deployment", SearchResultKind.Transaction, this.Hash.ToString());
                            break;
                        }

                    case EventKind.PlatformCreate:
                        {
                            var name = evt.GetContent<string>();
                            sb.AppendLine($"{LinkAddress(evt.Address)} created platform: {name}");
                            Nexus.RegisterSearch(name, "Platform Creation", SearchResultKind.Transaction, this.Hash.ToString());
                            break;
                        }

                    case EventKind.OrganizationCreate:
                        {
                            var name = evt.GetContent<string>();
                            sb.AppendLine($"{LinkAddress(evt.Address)} created organization: {name}");
                            Nexus.RegisterSearch(name, "Organization Creation", SearchResultKind.Transaction, this.Hash.ToString());
                            break;
                        }

                    case EventKind.Crowdsale:
                        {
                            var data = evt.GetContent<SaleEventData>();

                            var sale = this.Nexus.FindSaleByHash(data.saleHash);

                            if (sale != null)
                            {
                                switch (data.kind)
                                {
                                    case SaleEventKind.Creation:
                                        sb.AppendLine($"{LinkAddress(evt.Address, null)} created crowdsale: {LinkSale(sale)}");
                                        break;

                                    case SaleEventKind.AddedToWhitelist:
                                        sb.AppendLine($"Added {LinkAddress(evt.Address)} to whitelist: {LinkSale(sale)}");
                                        break;

                                    case SaleEventKind.RemovedFromWhitelist:
                                        sb.AppendLine($"Removed {LinkAddress(evt.Address)} from whitelist of  {LinkSale(sale)}");
                                        break;

                                    case SaleEventKind.Participation:
                                        {
                                            string extra = "";

                                            var other = Events.FirstOrDefault(x => x.Kind == EventKind.TokenStake && x.Contract == evt.Contract);
                                            if (other.Kind == EventKind.TokenStake)
                                            {
                                                var otherData = other.GetContent<TokenEventData>();
                                                var token = Nexus.FindTokenBySymbol(otherData.Symbol);

                                                extra = $" with {UnitConversion.ToDecimal(otherData.Value, token != null ? token.Decimals : 0)} {LinkToken(otherData.Symbol)}";    
                                            }

                                            sb.AppendLine($"{LinkAddress(evt.Address)} participated in {LinkSale(sale)}{{extra}}");
                                            break;
                                        }

                                    case SaleEventKind.SoftCap:
                                        sb.AppendLine($"Soft-cap reached: {LinkSale(sale)}");
                                        break;

                                    case SaleEventKind.HardCap:
                                        sb.AppendLine($"Hard-cap reached: {LinkSale(sale)}");
                                        break;

                                    case SaleEventKind.Distribution:
                                        sb.AppendLine($"Sale distribution: {LinkSale(sale)}");
                                        break;

                                    case SaleEventKind.Refund:
                                        sb.AppendLine($"Sale refund: {LinkSale(sale)}");
                                        break;
                                }
                            }
                            
                            break;
                        }

                    case EventKind.ChainSwap:
                        {
                            var data = evt.GetContent<TransactionSettleEventData>();
                            if (data.Platform == "neo")
                            {
                              sb.AppendLine($"Settled {data.Platform} transaction <a href=\"https://neoscan.io/transaction/{data.Hash}\" target=\"_blank\">{data.Hash}</a>");
                            }
                            else if (data.Platform == "ethereum")
                            {
                              sb.AppendLine($"Settled {data.Platform} transaction <a href=\"https://etherscan.io/tx/0x{data.Hash}\" target=\"_blank\">{data.Hash}</a>");
                            }
                            else if (data.Platform == "bsc")
                            {
                              sb.AppendLine($"Settled {data.Platform} transaction <a href=\"https://bscscan.com/tx/0x{data.Hash}\" target=\"_blank\">{data.Hash}</a>");
                            }
                            else
                            {
                              sb.AppendLine($"Settled {data.Platform} transaction <a href=\"/tx/{data.Hash}\">{data.Hash}</a>");
                            }

                            Nexus.RegisterSearch(data.Hash.ToString(), "Settlement", SearchResultKind.Transaction, this.Hash.ToString());
                            break;
                        }

                    case EventKind.ValidatorElect:
                        {
                            var address = evt.GetContent<Address>();
                            sb.AppendLine($"{LinkAddress(address)} was elected validator");
                            break;
                        }

                    case EventKind.ValidatorPropose:
                        {
                            var address = evt.GetContent<Address>();
                            sb.AppendLine($"{LinkAddress(address)} was proposed as validator");
                            break;
                        }

                    case EventKind.ValidatorSwitch:
                        {
                            sb.AppendLine($"Switched current validator to {LinkAddress(evt.Address)}");
                            break;
                        }

                    case EventKind.ValueCreate:
                        {
                            var data = evt.GetContent<ChainValueEventData>();
                            sb.AppendLine($"{LinkAddress(evt.Address)} created governance value: {data.Name}");
                            Nexus.RegisterSearch(data.Name, "Value Creation", SearchResultKind.Transaction, this.Hash.ToString());
                            break;
                        }

                    case EventKind.ValueUpdate:
                        {
                            var data = evt.GetContent<ChainValueEventData>();
                            sb.AppendLine($"{LinkAddress(evt.Address)} updated governance value: {data.Name}");
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
                                var operation = (evt.Contract == "stake" && data.Symbol == DomainSettings.FuelTokenSymbol) ? "claimed" : "minted";
                                sb.AppendLine($"{LinkAddress(evt.Address)} {operation} {UnitConversion.ToDecimal(data.Value, token != null ? token.Decimals : 0)} {LinkToken(data.Symbol)}");
                            }
                            else if (data.Symbol == "TTRS")
                            {
                                sb.AppendLine($"{LinkAddress(evt.Address)} minted {LinkToken(data.Symbol)} - NFT <a href=\"https://www.22series.com/part_info?id={data.Value}\" target=\"_blank\">#{data.Value}</a>");
                            }
                            else if (data.Symbol == "GHOST")
                            {
                                sb.AppendLine($"{LinkAddress(evt.Address)} minted {LinkToken(data.Symbol)} - NFT <a href=\"https://ghostmarket.io/asset/pha/ghost/{data.Value}\" target=\"_blank\">#{data.Value}</a>");
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
                            else if (data.Symbol == "TTRS")
                            {
                                sb.AppendLine($"{LinkAddress(evt.Address)} burned {LinkToken(data.Symbol)} - NFT <a href=\"https://www.22series.com/part_info?id={data.Value}\" target=\"_blank\">#{data.Value}</a>");
                            }
                            else if (data.Symbol == "GHOST")
                            {
                                sb.AppendLine($"{LinkAddress(evt.Address)} burned {LinkToken(data.Symbol)} - NFT <a href=\"https://ghostmarket.io/asset/pha/ghost/{data.Value}\" target=\"_blank\">#{data.Value}</a>");
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
                            var contractAddress = Address.FromHash(evt.Contract);
                            bool fungible = token.IsFungible();
                            if (fungible)
                            {
                                sb.AppendLine($"{LinkAddress(evt.Address)} claimed {UnitConversion.ToDecimal(data.Value, token != null ? token.Decimals : 0)} {LinkToken(data.Symbol)}");
                            }
                            else if (data.Symbol == "TTRS")
                            {
                                sb.AppendLine($"{LinkAddress(evt.Address)} claimed {LinkToken(data.Symbol)} - NFT <a href=\"https://www.22series.com/part_info?id={data.Value}\" target=\"_blank\">#{data.Value}</a>");
                            }
                            else if (data.Symbol == "GHOST")
                            {
                                sb.AppendLine($"{LinkAddress(evt.Address)} claimed {LinkToken(data.Symbol)} - NFT <a href=\"https://ghostmarket.io/asset/pha/ghost/{data.Value}\" target=\"_blank\">#{data.Value}</a>");
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
                            var contractAddress = Address.FromHash(evt.Contract);
                            bool fungible = token.IsFungible();

                            switch (evt.Contract)
                            {
                                case "gas":
                                case "swap":
                                case "stake":
                                case "market":
                                    if (fungible)
                                    {
                                        if ((evt.Address).IsInterop) // used to check if from external chain
                                        {
                                            sb.AppendLine($"{LinkAddress(evt.Address)} withdrew {UnitConversion.ToDecimal(data.Value, token != null ? token.Decimals : 0)} {LinkToken(data.Symbol)} from {LinkAddress(contractAddress, evt.Contract)} contract");
                                        }
                                        else
                                        {
                                            sb.AppendLine($"{LinkAddress(evt.Address)} deposited {UnitConversion.ToDecimal(data.Value, token != null ? token.Decimals : 0)} {LinkToken(data.Symbol)} into {LinkAddress(contractAddress, evt.Contract)} contract");
                                        }
                                    }
                                    else if (data.Symbol == "TTRS")
                                    {
                                        sb.AppendLine($"{LinkAddress(evt.Address)} deposited {LinkToken(data.Symbol)} - NFT <a href=\"https://www.22series.com/part_info?id={data.Value}\" target=\"_blank\">#{data.Value}</a> into {LinkAddress(contractAddress, evt.Contract)} contract");
                                    }
                                    else if (data.Symbol == "GHOST")
                                    {
                                        sb.AppendLine($"{LinkAddress(evt.Address)} deposited {LinkToken(data.Symbol)} - NFT <a href=\"https://ghostmarket.io/asset/pha/ghost/{data.Value}\" target=\"_blank\">#{data.Value}</a> into {LinkAddress(contractAddress, evt.Contract)} contract");
                                    }
                                    else
                                    {
                                        sb.AppendLine($"{LinkAddress(evt.Address)} deposited {LinkToken(data.Symbol)} - NFT #{data.Value} into {LinkAddress(contractAddress, evt.Contract)} contract");
                                    }
                                    break;

                                default:
                                    // non native contracts, already handled by infusion
                                    break;
                            }

                            break;
                        }

                    case EventKind.TokenSend:
                        {
                            var data = evt.GetContent<TokenEventData>();
                            var token = Nexus.FindTokenBySymbol(data.Symbol);
                            bool fungible = token.IsFungible();

                            string destination = "";

                            if (k < Events.Length - 1)
                            {
                                var nextEvt = Events[k + 1];
                                if (nextEvt.Kind == EventKind.TokenReceive)
                                {
                                    var nextData = nextEvt.GetContent<TokenEventData>();
                                    if (nextData.Symbol == data.Symbol && nextData.Value == data.Value)
                                    {
                                        destination = " to " + LinkAddress(nextEvt.Address);
                                        skipEvent = true;
                                    }
                                }
                            }

                            if (evt.Address.IsInterop)
                            {
                                string addressText = null;

                                switch (evt.Address.PlatformID)
                                {
                                    /*
                                    case NeoWallet.NeoID:
                                        {
                                            addressText = Pay.Chains.NeoWallet.DecodeAddress(evt.Address);
                                            break;
                                        }

                                    case EthereumWallet.EthereumID:
                                        {
                                            addressText = Pay.Chains.EthereumWallet.DecodeAddress(evt.Address);
                                            break;
                                        }*/

                                    /* case BscWallet.BscID:
                                        {
                                            addressText = Pay.Chains.BscWallet.DecodeAddress(evt.Address);
                                            break;
                                        } */
                                }

                                if (!string.IsNullOrEmpty(addressText))
                                {
                                    Nexus.RegisterSearch(addressText, "Chain Swap", SearchResultKind.Transaction, this.Hash.ToString());
                                }
                            }

                            if (fungible)
                            {
                                sb.AppendLine($"{LinkAddress(evt.Address)} sent {UnitConversion.ToDecimal(data.Value, token != null ? token.Decimals : 0)} {LinkToken(data.Symbol)}{destination}");
                            }
                            else
                            {
                                string url = null;

                                switch (data.Symbol)
                                {
                                    case "TTRS":
                                        url = $"https://www.22series.com/part_info?id={data.Value}";
                                        break;

                                    case "GHOST":
                                        url = $"https://ghostmarket.io/asset/pha/ghost/{data.Value}";
                                        break;
                                }

                                if (url != null)
                                {
                                    sb.AppendLine($"{LinkAddress(evt.Address)} sent {LinkToken(data.Symbol)} - NFT <a href=\"{url}\" target=\"_blank\">#{data.Value}</a>{destination}");
                                }
                                else
                                {
                                    sb.AppendLine($"{LinkAddress(evt.Address)} sent {LinkToken(data.Symbol)} - NFT #{data.Value}{destination}");
                                }
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
                            else if (data.Symbol == "TTRS")
                            {
                                sb.AppendLine($"{LinkAddress(evt.Address)} received {LinkToken(data.Symbol)} - NFT <a href=\"https://www.22series.com/part_info?id={data.Value}\" target=\"_blank\">#{data.Value}</a>");
                            }
                            else if (data.Symbol == "GHOST")
                            {
                                sb.AppendLine($"{LinkAddress(evt.Address)} received {LinkToken(data.Symbol)} - NFT <a href=\"https://ghostmarket.io/asset/pha/ghost/{data.Value}\" target=\"_blank\">#{data.Value}</a>");
                            }
                            else
                            {
                                sb.AppendLine($"{LinkAddress(evt.Address)} received {LinkToken(data.Symbol)} - NFT #{data.Value}");
                            }
                            break;
                        }

                    case EventKind.Infusion:
                    {
                        var data = evt.GetContent<InfusionEventData>();
                        var token = Nexus.FindTokenBySymbol(data.BaseSymbol);
                        var tokenInfused = Nexus.FindTokenBySymbol(data.InfusedSymbol);
                        bool fungible = tokenInfused.IsFungible();

                        if (fungible)
                        {
                            if (token.Symbol == "TTRS")
                            {
                                sb.AppendLine($"{LinkAddress(evt.Address)} infused {LinkToken(token.Symbol)} - NFT <a href=\"https://www.22series.com/part_info?id={data.TokenID}\" target=\"_blank\">#{data.TokenID}</a> with {UnitConversion.ToDecimal(data.InfusedValue, tokenInfused != null ? tokenInfused.Decimals : 0)} {LinkToken(data.InfusedSymbol)}");
                            }
                            else if (token.Symbol == "GHOST")
                            {
                                sb.AppendLine($"{LinkAddress(evt.Address)} infused {LinkToken(token.Symbol)} - NFT <a href=\"https://ghostmarket.io/asset/pha/ghost/{data.TokenID}\" target=\"_blank\">#{data.TokenID}</a> with {UnitConversion.ToDecimal(data.InfusedValue, tokenInfused != null ? tokenInfused.Decimals : 0)} {LinkToken(data.InfusedSymbol)}");
                            }
                            else
                            {
                                sb.AppendLine($"{LinkAddress(evt.Address)} infused {LinkToken(token.Symbol)} - NFT #{data.TokenID} with {UnitConversion.ToDecimal(data.InfusedValue, tokenInfused != null ? tokenInfused.Decimals : 0)} {LinkToken(data.InfusedSymbol)}");
                            }
                        }
                        else
                        {
                            if (token.Symbol == "TTRS")
                            {
                                if (data.InfusedSymbol == "TTRS")
                                {
                                    sb.AppendLine($"{LinkAddress(evt.Address)} infused {LinkToken(token.Symbol)} - NFT <a href=\"https://www.22series.com/part_info?id={data.TokenID}\" target=\"_blank\">#{data.TokenID}</a> with {LinkToken(data.InfusedSymbol)} - NFT <a href=\"https://www.22series.com/part_info?id={data.InfusedValue}\" target=\"_blank\">#{data.InfusedValue}</a>");
                                }
                                else if (data.InfusedSymbol == "GHOST")
                                {
                                    sb.AppendLine($"{LinkAddress(evt.Address)} infused {LinkToken(token.Symbol)} - NFT <a href=\"https://www.22series.com/part_info?id={data.TokenID}\" target=\"_blank\">#{data.TokenID}</a> with {LinkToken(data.InfusedSymbol)} - NFT <a href=\"https://ghostmarket.io/asset/pha/ghost/{data.InfusedValue}\" target=\"_blank\">#{data.InfusedValue}</a>");
                                }
                                else
                                {
                                    sb.AppendLine($"{LinkAddress(evt.Address)} infused {LinkToken(token.Symbol)} - NFT <a href=\"https://www.22series.com/part_info?id={data.TokenID}\" target=\"_blank\">#{data.TokenID}</a> with {LinkToken(data.InfusedSymbol)} - NFT #{data.InfusedValue}");
                                }
                            }
                            else if (token.Symbol == "GHOST")
                            {
                              if (data.InfusedSymbol == "TTRS")
                              {
                                  sb.AppendLine($"{LinkAddress(evt.Address)} infused {LinkToken(token.Symbol)} - NFT <a href=\"https://ghostmarket.io/asset/pha/ghost/{data.TokenID}\" target=\"_blank\">#{data.TokenID}</a> with {LinkToken(data.InfusedSymbol)} - NFT <a href=\"https://www.22series.com/part_info?id={data.InfusedValue}\" target=\"_blank\">#{data.InfusedValue}</a>");
                              }
                              else if (data.InfusedSymbol == "GHOST")
                              {
                                  sb.AppendLine($"{LinkAddress(evt.Address)} infused {LinkToken(token.Symbol)} - NFT <a href=\"https://ghostmarket.io/asset/pha/ghost/{data.TokenID}\" target=\"_blank\">#{data.TokenID}</a> with {LinkToken(data.InfusedSymbol)} - NFT <a href=\"https://ghostmarket.io/asset/pha/ghost/{data.InfusedValue}\" target=\"_blank\">#{data.InfusedValue}</a>");
                              }
                              else
                              {
                                  sb.AppendLine($"{LinkAddress(evt.Address)} infused {LinkToken(token.Symbol)} - NFT <a href=\"https://ghostmarket.io/asset/pha/ghost/{data.TokenID}\" target=\"_blank\">#{data.TokenID}</a> with {LinkToken(data.InfusedSymbol)} - NFT #{data.InfusedValue}");
                              }
                            }
                            else
                            {
                                sb.AppendLine($"{LinkAddress(evt.Address)} infused {LinkToken(token.Symbol)} - NFT #{data.TokenID} with {LinkToken(data.InfusedSymbol)} - NFT #{data.InfusedValue}");
                            }
                        }
                        break;
                    }

                    case EventKind.FileCreate:
                        {
                            var hash = evt.GetContent<Hash>();
                            sb.AppendLine($"{LinkAddress(evt.Address)} created file {LinkFile(hash)}");
                            break;
                        }

                    case EventKind.FileDelete:
                        {
                            var hash = evt.GetContent<Hash>();
                            sb.AppendLine($"{LinkAddress(evt.Address)} deleted file {LinkFile(hash)}");
                            break;
                        }

                    default:
                        {
                            if (evt.Kind >= EventKind.Custom)
                            {
                                try
                                {
                                    var contract = this.Nexus.FindContract(DomainSettings.RootChainName, evt.Contract);
                                    var contractEvent = contract.Events.Where(x => x.value == (byte)evt.Kind).First();
                                    var vm = new CustomDescriptionVM(this.Nexus, contractEvent.description);
                                    vm.Stack.Push(VMObject.FromObject(evt.Data));
                                    vm.Stack.Push(VMObject.FromObject(evt.Address));
                                    vm.Execute();

                                    var result = vm.Stack.Pop().AsString();
                                    sb.AppendLine(result);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e.Message);
                                }


                            }
                            break;
                        }
                }
            }


            foreach (var addr in addresses)
            {
                var account = Nexus.FindAccount(addr, false);
                if (account != null)
                {
                    if (!account.Transactions.Exists(e => e.Hash == Hash))
                    {
                        if(DateTime.Compare(account.Transactions.First().Timestamp, Timestamp) <= 0)
                        {
                            // This transaction is newer (or same time) then all others, we should insert at the start of the list.
                            account.Transactions.Insert(0, new TransactionForDisplay(Hash, Timestamp));
                        }
                        else if(DateTime.Compare(account.Transactions.Last().Timestamp, Timestamp) >= 0)
                        {
                            // This transaction is older (or same time) then all others, we should insert at the end of the list.
                            account.Transactions.Add(new TransactionForDisplay(Hash, Timestamp));
                        }
                        else
                        {
                            int index = account.Transactions.FindIndex(e => DateTime.Compare(e.Timestamp, Timestamp) <= 0);
                            if(index != -1)
                            {
                                // Inserting transaction in the middle of the list.
                                account.Transactions.Insert(index, new TransactionForDisplay(Hash, Timestamp));
                            }
                            else
                            {
                                // Impossible really.
                                account.Transactions.Add(new TransactionForDisplay(Hash, Timestamp));
                            }
                        }
                    }

                    //Nexus._addresses.Add(addr);
                }
            }

            if (sb.Length > 0)
            {
                _description = sb.ToString().Replace("\n", "<br>");
            }
            else
            {
                _description = "Custom Transaction";
            }

            sb.Clear();

            if (!feeAddress.IsNull)
            {
                sb.AppendLine($"{LinkAddress(feeAddress)} paid {totalFees} {LinkToken(DomainSettings.FuelTokenSymbol)} in fees.");
            }

            foreach (var entry in fees)
            {
                sb.AppendLine($"{LinkAddress(entry.Key)} received {entry.Value} {LinkToken(DomainSettings.FuelTokenSymbol)} in fees.");
            }

            if (!feeAddress.IsNull)
            {
                if (crownFees != 0)
                {
                    sb.AppendLine($"{LinkAddress(feeAddress)} paid {crownFees} {LinkToken(DomainSettings.FuelTokenSymbol)} to crown rewards.");
                }
                sb.AppendLine($"{LinkAddress(feeAddress)} burned {burnedFees} {LinkToken(DomainSettings.FuelTokenSymbol)}.");
            }

            if (sb.Length > 0)
            {
                _fees = sb.ToString().Replace("\n", "<br>");
            }
            else
            {
                _fees = "No fees for this transaction.";
            }
        }
    }

    public class ChainData : ExplorerObject//, IChain
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
                Contracts = new string[contractsNodes.ChildCount];
                for (int i=0; i<Contracts.Length; i++)
                {
                    var name = contractsNodes.GetString(i);
                    Contracts[i] = name;

                    var address = SmartContract.GetAddressFromContractName(name);
                    Nexus.RegisterSearch(name, name, SearchResultKind.Address, address.Text);
                }
            }
            else
            {
                Contracts = new string[0];
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
                while (true)
                {
                    try
                    {
                        var ofs = index + currentHeight;
                        var block = Nexus.FindBlockByHeight(this, ofs + 1);
                        RegisterBlock(ofs, block);

                        break;
                    }
                    catch (Exception e)
                    {
                        e = e.ExpandInnerExceptions();

                        var logMessage = "RegisterBlock(): Exception caught:\n" + e.Message;
                        logMessage += "\n\n" + e.StackTrace;

                        Console.WriteLine(logMessage);
                        Thread.Sleep(1000);
                    }
                }
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

        public string[] Contracts { get; private set; }

        public List<BlockData> BlockList { get; private set; }

        public BlockData LastBlock => BlockList[BlockList.Count - 1];

        public IEnumerable<BlockData> Blocks => BlockList.Skip(Math.Max(0, (int)(Height - 100))).Reverse();
    }

    public class TokenData : ExplorerObject, IToken
    {
        public TokenData(NexusData database, DataNode node) : base(database)
        {
            this.Name = node.GetString("name");
            this.Symbol = node.GetString("symbol");
            this.MaxSupply = BigInteger.Parse(node.GetString("maxSupply"));
            this.CurrentSupply = BigInteger.Parse(node.GetString("currentSupply"));
            this.Owner = Address.FromText(node.GetString("owner"));
            this.Decimals = int.Parse(node.GetString("decimals"));
            this.Flags = node.GetEnum<TokenFlags>("flags");
            this.Script = Base16.Decode(node.GetString("script"));
            this.ABI = new ContractInterface();
        }

        public string Name { get; private set; }
        public string Symbol { get; private set; }
        public Address Owner { get; private set; }
        public TokenFlags Flags { get; private set; }
        public BigInteger MaxSupply { get; private set; }
        public BigInteger CurrentSupply { get; private set; }
        public int Decimals { get; private set; }
        public byte[] Script { get; private set; }
        public ContractInterface ABI { get; private set; }

        public string FormattedMaxSupply => (UnitConversion.ToDecimal(MaxSupply, Decimals)).ToString("#,##0");
        public string FormattedCurrentSupply => (UnitConversion.ToDecimal(CurrentSupply, Decimals)).ToString("#,##0");

        public decimal Price => CoinUtils.GetCoinRate(Symbol, "usd");

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

    public class SaleData : ExplorerObject
    {
        public Hash Hash { get; private set; }
        public Address Creator { get; private set; }
        public string Name { get; private set; }
        public SaleFlags Flags { get; private set; }
        public Timestamp StartDate { get; private set; }
        public Timestamp EndDate { get; private set; }

        public string SellSymbol { get; private set; }
        public string ReceiveSymbol { get; private set; }
        public BigInteger Price { get; private set; }
        public BigInteger GlobalSoftCap { get; private set; }
        public BigInteger GlobalHardCap { get; private set; }
        public BigInteger UserSoftCap { get; private set; }
        public BigInteger UserHardCap { get; private set; }

        public SaleData(NexusData database, DataNode node) : base(database)
        {
            this.Hash = Hash.Parse(node.GetString("hash"));
            this.Name = node.GetString("name");
            this.Creator = Address.FromText(node.GetString("creator"));
            this.Flags = node.GetEnum<SaleFlags>("flags");
            this.StartDate = new Timestamp(node.GetUInt32("startDate"));
            this.EndDate = new Timestamp(node.GetUInt32("endDate"));

            this.ReceiveSymbol = node.GetString("receiveSymbol");
            this.SellSymbol = node.GetString("sellSymbol");

            this.Price = BigInteger.Parse(node.GetString("price"));
            this.GlobalHardCap = BigInteger.Parse(node.GetString("globalHardCap"));
            this.GlobalSoftCap = BigInteger.Parse(node.GetString("globalSoftCap"));
            this.UserHardCap = BigInteger.Parse(node.GetString("userHardCap"));
            this.UserSoftCap = BigInteger.Parse(node.GetString("userSoftCap"));
        }
    }

    public class BalanceData: ExplorerObject
    {
        public ChainData Chain { get; private set; }
        public BigInteger Amount { get; private set; }
        public string Symbol { get; private set; }
        public BigInteger[] IDs { get; private set; }

        private int decimals;

        public string FormattedAmount => (UnitConversion.ToDecimal(Amount, decimals)).ToString("#,##0");
        public string FormattedAmountDecimals => (UnitConversion.ToDecimal(Amount, decimals)).ToString("#,##0.00");
        public decimal Value => (UnitConversion.ToDecimal(Amount, decimals))*(CoinUtils.GetCoinRate(Symbol, "usd"));
        public string FormattedValue => Value.ToString("#,##0.00");
        public bool IsTokenFungible => Nexus.FindTokenBySymbol(Symbol).IsFungible();

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

    public class TransactionForDisplay
    {
        public Hash Hash { get; set; }
        public DateTime Timestamp { get; set; }
        public string Date { get; set; }
        public TransactionForDisplay(Hash hash, DateTime timestamp)
        {
            Hash = hash;
            Timestamp = timestamp;
            Date = Timestamp.ToString("yyyy/MM/dd HH:mm:ss tt");
        }
    }

    public class AccountData : ExplorerObject
    {
        public AccountData(NexusData database, DataNode node) : base(database)
        {
            Name = node.GetString("name");
            Address = Address.FromText(node.GetString("address"));

            var stakeNode = node.GetNode("stakes");
            Stake = BigInteger.Parse(stakeNode.GetString("amount"));
            Unclaimed = BigInteger.Parse(stakeNode.GetString("unclaimed"));

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

            Transactions = new List<TransactionForDisplay>();

            var txsNode = node.GetNode("txs");
            if (txsNode != null)
            {
                for (int i = 0; i < txsNode.ChildCount; i++)
                {
                    Hash hash = Hash.Parse(txsNode.GetString(i));

                    var tx = Nexus.FindTransaction(null, hash);

                    if (tx != null)
                    {
                        Transactions.Add(new TransactionForDisplay(hash, tx.Timestamp));
                    }
                    else
                    {
                        Transactions.Add(new TransactionForDisplay(hash, new DateTime(0)));
                    }
                    Transactions.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));
                }
            }

            LastTime = DateTime.UtcNow;
        }

        internal DateTime LastTime { get; private set; }

        public string Name { get; private set; }
        public Address Address { get; private set; }

        public BigInteger Stake { get; private set; }
        public BigInteger Unclaimed { get; private set; }
        public string FormattedStake => (UnitConversion.ToDecimal(Stake, DomainSettings.StakingTokenDecimals)).ToString("#,##0");
        public string FormattedUnclaimed => (UnitConversion.ToDecimal(Unclaimed, DomainSettings.FuelTokenDecimals)).ToString("#,##0");

        public BalanceData[] Balances { get; private set; }

        public List<TransactionForDisplay> Transactions { get; internal set; }

        public bool IsEmpty
        {
            get
            {
                if (Address.Kind != AddressKind.User)
                {
                    return false;
                }

                return Name==ValidationUtils.ANONYMOUS_NAME && Stake == 0 && Balances.Length == 0;
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

    public struct ContractMethodParameter
    {
        public string name;
        public string type;
    }

    public struct ContractMethod
    {
        public string name;
        public string returnType;
        public ContractMethodParameter[] parameters;
    }

    public struct ContractEvent
    {
        public byte value;
        public string name;
        public string returnType;
        public byte[] description;
    }

    public class ContractData : ExplorerObject
    {
        public ContractData(NexusData database, DataNode node) : base(database)
        {
            ID = node.GetString("name");
            Script = Base16.Decode(node.GetString("script"));

            var methods = new List<ContractMethod>();
            var methodNode = node.GetNode("methods");
            if (methodNode != null)
            {
                foreach (var child in methodNode.Children)
                {
                    var method = new ContractMethod();
                    method.name = child.GetString("name");
                    method.returnType = child.GetString("returnType");

                    var parameters = new List<ContractMethodParameter>();

                    var parametersNode = child.GetNode("parameters");
                    if (parametersNode != null)
                    {
                        foreach (var entry in parametersNode.Children)
                        {
                            var parameter = new ContractMethodParameter();
                            parameter.name = entry.GetString("name");
                            parameter.type = entry.GetString("type");

                            parameters.Add(parameter);
                        }
                    }

                    method.parameters = parameters.ToArray();

                    methods.Add(method);
                }
            }
            this.Methods = methods.ToArray();

            var events = new List<ContractEvent>();
            var eventNode = node.GetNode("events");
            if (eventNode != null)
            {
                foreach (var child in eventNode.Children)
                {
                    var evt = new ContractEvent();
                    evt.value = child.GetByte("value");
                    evt.name = child.GetString("name");
                    evt.description = Base16.Decode(child.GetString("description"));
                    evt.returnType = child.GetString("returnType");
                    events.Add(evt);
                }
            }
            this.Events = events.ToArray();

            this.Address = Address.FromText(node.GetString("address"));

            var disasm = new Disassembler(this.Script);
            this.Instructions = disasm.Instructions.ToArray();

            NativeContractKind temp;
            if (Enum.TryParse<NativeContractKind>(ID, true, out temp))
            {
                this.Native = temp.ToString();
            }
            else
            {
                this.Native = null;
            }
        }

        public string ID { get; private set; }

        public byte[] Script { get; private set; }

        public ContractMethod[] Methods { get; private set; }
        public ContractEvent[] Events { get; private set; }


        public Address Address { get; private set; }

        public Instruction[] Instructions { get; set; }

        public string Native;
    }

    public class FileData : ExplorerObject
    {
        public FileData(NexusData database, DataNode node) : base(database)
        {
            ID = node.GetString("hash");
            Name = node.GetString("name");
            Time = new Timestamp(node.GetUInt32("time"));
            Size = uint.Parse(node.GetUInt32("size").ToString()) / 1024;
        }

        public string ID { get; private set; }

        public string Name { get; private set; }

        public Timestamp Time { get; private set; }

        public uint Size { get; private set; }
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
        private Dictionary<string, ContractData> _contracts = new Dictionary<string, ContractData>();
        private Dictionary<string, FileData> _files = new Dictionary<string, FileData>();

        private Dictionary<Hash, BlockData> _blocks = new Dictionary<Hash, BlockData>();
        private Dictionary<Hash, TransactionData> _transactions = new Dictionary<Hash, TransactionData>();
        
        private Dictionary<Hash, SaleData> _sales = new Dictionary<Hash, SaleData>();

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

        public readonly string cachePath;

        // Used for hack to make organizations update run not as often as blocks update.
        private int updateCyclesCounter = 0;

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
            updateCyclesCounter++;

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

            Console.WriteLine($"Updating {_chains.Count} chains...");
            foreach (var chain in _chains.Values)
            {
                this.UpdateStatus = "Fetching blocks for chain " + chain.Name;
                this.UpdateProgress = 0;

                chain.UpdateBlocks();
            }

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

                        this.UpdateProgress = (current * 100) / total;
                    }

                    Console.Write($"Finished generating {total} tx descriptions");
                }).Start();
            }

            if (updateCyclesCounter >= 10)
            {
                var orgNode = node.GetNode("organizations");
                if (orgNode != null)
                {
                    foreach (var entry in orgNode.Children)
                    {
                        var name = entry.Value;
                        if (!CheckOrganization(name))
                        {
                            FindOrganization(name);
                        }
                        else
                        {
                            UpdateOrganization(name);
                        }
                    }
                }

                this.UpdateStatus = "Fetching master accounts";
                this.UpdateProgress = 0;
                if (CheckOrganization("masters"))
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

                updateCyclesCounter = 0;
            }

            return true;
        }

        public DataNode APIRequest(string path)
        {
            var url = RESTurl + path;
            //Console.WriteLine("APIRequest: url: " + url);

            int max = 5;
            for (int i=1; i<=max; i++)
            {
                try
                {
                    string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                    Console.WriteLine(timestamp + ": APIRequest: " + url);

                    string contents;
                    using (var wc = new System.Net.WebClient())
                    {
                        contents = wc.DownloadString(url);
                    }

                    //Console.WriteLine("APIRequest: response: " + contents);

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

        public bool CheckOrganization(string id)
        {
            return _organizations.ContainsKey(id);
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

        public ContractData FindContract(string chain, string id)
        {
            if (_contracts.ContainsKey(id))
            {
                return _contracts[id];
            }

            Console.WriteLine("Detected new contract: " + id);
            var temp = APIRequest($"getContract/{chain}/{id}");
            var contract = new ContractData(this, temp);
            _contracts[id] = contract;

            RegisterSearch(id, id, SearchResultKind.Contract, id);

            return contract;
        }

        public FileData FindFile(string id)
        {
            if (_files.ContainsKey(id))
            {
                return _files[id];
            }

            Console.WriteLine("Detected new file: " + id);
            var temp = APIRequest($"getArchive/{id}");
            var file = new FileData(this, temp);
            _files[id] = file;

            RegisterSearch(id, file.Name, SearchResultKind.File, id);

            return file;
        }

        public TransactionData FindTransaction(ChainData chain, Hash txHash)
        {
            if (_transactions.ContainsKey(txHash))
            {
                return _transactions[txHash];
            }

            var fileName = GetTransactionCacheFileName(txHash);
            if (File.Exists(fileName))
            {
                var xml = File.ReadAllText(fileName);
                var temp = XMLReader.ReadFromString(xml);
                temp = temp.GetNodeByIndex(0);
                var tx = new TransactionData(this, temp);

                lock (_transactions)
                {
                    _transactions[tx.Hash] = tx;
                }

                lock (_transactionQueue)
                {
                    _transactionQueue.Enqueue(tx);
                }

                RegisterSearch(txHash.ToString(), null, SearchResultKind.Transaction);

                Console.WriteLine("Loaded transaction from cache (find): " + txHash);

                return tx;
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

                fileName = GetTransactionCacheFileName(txHash);
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

            var dir = $"{cachePath}{bucket}";
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

        internal SaleData FindSaleByHash(Hash hash)
        {
            lock (_sales)
            {
                if (_sales.ContainsKey(hash))
                {
                    return _sales[hash];
                }
            }

            var node = this.APIRequest("getSale/" + hash);

            if (node != null)
            {
                var sale = new SaleData(this, node);

                lock (_sales)
                {
                    _sales[hash] = sale;
                }

                RegisterSearch(sale.Name, null, SearchResultKind.Sale);
                return sale;
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

                try
                {
                    account = new AccountData(this, node);
                }
                catch(Exception e)
                {
                    Console.WriteLine($"FindAccount({address}, {canExpire}): Exception occurred: {e}");
                    return null;
                }
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
