using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Phantasma.Blockchain.Contracts;
using Phantasma.Blockchain.Contracts.Native;
using Phantasma.Blockchain.Tokens;
using Phantasma.Cryptography;
using Phantasma.Explorer.Infrastructure.Interfaces;
using Phantasma.Explorer.Infrastructure.Models;
using Phantasma.IO;
using Phantasma.Numerics;
using Phantasma.RpcClient;
using Phantasma.RpcClient.DTOs;
using Phantasma.RpcClient.Interfaces;
using Event = Phantasma.Blockchain.Contracts.Event;

namespace Phantasma.Explorer.Infrastructure.Data
{
    public class MockRepository : IRepository
    {
        private RootChainDto _rootChain;
        private readonly List<ChainDataAccess> _chains = new List<ChainDataAccess>();
        private readonly Dictionary<string, TokenDto> _tokens = new Dictionary<string, TokenDto>();
        private readonly Dictionary<TokenDto, int> _tokenTransfers = new Dictionary<TokenDto, int>();
        private readonly List<Address> _addresses = new List<Address>();

        private bool _isInitFinish;
        private const int NativeTokenDecimals = 8;
        private const string PlatformName = "Phantasma";

        public List<AppDto> Apps { get; set; }

        private IPhantasmaRpcService _phantasmaRpcService;

        public async Task InitRepo()
        {
            _phantasmaRpcService = new PhantasmaRpcService(new RpcClient.Client.RpcClient(new Uri("http://localhost:7077/rpc")));

            var root = await _phantasmaRpcService.GetRootChain.SendRequestAsync();
            var chains = await _phantasmaRpcService.GetChains.SendRequestAsync(); //name-address info only
            var tokens = await _phantasmaRpcService.GetTokens.SendRequestAsync();
            var appList = await _phantasmaRpcService.GetApplications.SendRequestAsync();

            Apps = appList;
            _rootChain = root;

            foreach (var token in tokens)
            {
                _tokens.Add(token.Symbol, token);
            }

            // working

            foreach (var chain in chains)
            {
                var persistentChain = SetupChain(chain);
                var childs = chains.Where(p => p.ParentAddress.Equals(chain.Address));

                if (childs.Any())
                {
                    persistentChain.AddChildren(childs.ToList());
                }

                await SetupBlocks(persistentChain);
            }

            _isInitFinish = true;
        }

        public async Task SyncronizeNewBlocks()
        {
            if (!_isInitFinish) return;
            try
            {
                foreach (var chain in _chains)
                {
                    var height = await _phantasmaRpcService.GetBlockHeight.SendRequestAsync(chain.Address);
                    if (height > chain.Height)
                    {
                        Debug.WriteLine($"NEW BLOCK: Chain: {chain.Name}, block: {height}");
                        var block = await _phantasmaRpcService.GetBlockByHeight.SendRequestAsync(chain.Address, height);
                        SetupSingleBlock(chain, block);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception: " + ex.Message);
                await SyncronizeNewBlocks();//todo revisit logic
            }
        }

        public decimal GetAddressNativeBalance(Address address, string chainName = null)
        {
            if (string.IsNullOrEmpty(chainName))
            {
                return TokenUtils.ToDecimal(GetRootChain.GetTokenAddressBalance(GetNativeToken, address), GetNativeToken.Decimals);
            }
            var chain = GetChain(chainName);

            return TokenUtils.ToDecimal(chain.GetTokenAddressBalance(GetNativeToken, address), GetNativeToken.Decimals);
        }

        public decimal GetAddressBalance(Address address, TokenDto token, string chainName)
        {
            var chain = GetChain(chainName);
            decimal balance = 0;
            if (chain != null)
            {
                balance = TokenUtils.ToDecimal(chain.GetTokenAddressBalance(token, address), token.Decimals);
            }

            return balance;
        }

        public string GetAddressName(string address)
        {
            return _phantasmaRpcService.GetAccount.SendRequestAsync(address).Result.Name;//todo remove rpc
        }

        public IEnumerable<Address> GetAddressList(int count = 20)
        {
            return _addresses.Take(count);
        }

        public ChainDataAccess GetChain(string chainInput)
        {
            return _chains.SingleOrDefault(p => p.Address.Equals(chainInput, StringComparison.InvariantCulture) || p.Name.Equals(chainInput, StringComparison.InvariantCulture));
        }

        public int GetChainCount()
        {
            return _chains.Count;
        }

        public IEnumerable<string> GetChainNames()
        {
            var nameList = new List<string>();
            foreach (var chain in _chains)
            {
                nameList.Add(chain.Name);
            }

            return nameList;
        }

        public IEnumerable<TransactionDto> GetTransactions(string chainAddress, int txAmount)
        {
            var txList = new List<TransactionDto>();
            var blocksList = new List<BlockDto>();
            if (string.IsNullOrEmpty(chainAddress)) //all chains
            {
                foreach (var chain in _chains)
                {
                    blocksList.AddRange(chain.GetBlocks.TakeLast(txAmount * 10));
                }

                blocksList = blocksList.OrderByDescending(b => b.Timestamp).ToList();

                foreach (var block in blocksList)
                {
                    foreach (var tx in block.Txs)
                    {
                        txList.Add(tx);
                        if (txList.Count == txAmount) return txList;
                    }
                }
            }
            else
            {
                var chain = GetChain(chainAddress);
                if (chain != null)
                {
                    foreach (var block in chain.GetBlocks)
                    {
                        foreach (var tx in block.Txs)
                        {
                            txList.Add(tx);
                            if (txList.Count == txAmount) return txList;
                        }
                    }
                }
            }

            return txList;
        }

        public IEnumerable<TransactionDto> GetAddressTransactions(Address address, int amount = 20)
        {
            var txs = _phantasmaRpcService.GetAddressTxs.SendRequestAsync(address.ToString(), amount).Result;//todo remove result
            return txs.Txs; //todo
        }

        public int GetAddressTransactionCount(Address address, string chainName)
        {
            return _phantasmaRpcService.GetAddressTxCount.SendRequestAsync(address.ToString(), chainName).Result;
        }

        public int GetTotalChainTransactionCount(string chainInput)
        {
            var chain = GetChain(chainInput);
            return chain.GetBlocks.SelectMany(p => p.Txs).Count(); //todo confirm this
        }

        public TransactionDto GetTransaction(string txHash)
        {
            var tx = FindTransactionByHash(txHash);
            return tx;
        }

        public int GetTotalTransactions()
        {
            int total = 0;
            foreach (var chain in _chains)
            {
                var blocks = chain.GetBlocks;
                total += blocks.Sum(p => p.Txs.Count);
            }

            return total;
        }

        public IEnumerable<TokenDto> GetTokens()
        {
            var tokens = _tokens.Values.ToList();
            return tokens;
        }

        public TokenDto GetToken(string symbol)
        {
            return _tokens[symbol];
        }

        public IEnumerable<TransactionDto> GetLastTokenTransfers(string symbol, int amount) //todo persist
        {
            return _phantasmaRpcService.GetTokenTransfers.SendRequestAsync(symbol, amount).Result;
        }

        public int GetTokenTransfersCount(string symbol) //todo persist
        {
            return _phantasmaRpcService.GetTokenTransferCount.SendRequestAsync(symbol).Result;
        }


        public string GetEventContent(BlockDto block, EventDto evt) //todo remove Native event dependency
        {
            Event nativeEvent;
            if (evt.Data != null)
            {
                nativeEvent = new Event((EventKind)evt.EvtKind,
                    Address.FromText(evt.EventAddress), evt.Data.Decode());
            }
            else
            {
                nativeEvent =
                    new Event((EventKind)evt.EvtKind, Address.FromText(evt.EventAddress));
            }

            switch (evt.EvtKind)
            {
                case EvtKind.ChainCreate:
                    {
                        var tokenData = nativeEvent.GetContent<TokenEventData>();
                        var chain = GetChainInfoByAddress(tokenData.chainAddress.ToString());
                        return $"{chain.Name} chain created at address <a href=\"/chain/{tokenData.chainAddress}\">{tokenData.chainAddress}</a>.";
                    }
                case EvtKind.TokenCreate:
                    {
                        var symbol = Serialization.Unserialize<string>(nativeEvent.Data);
                        var token = GetToken(symbol);
                        return $"{token.Name} token created with symbol <a href=\"/token/{symbol}\">{symbol}</a>.";
                    }
                case EvtKind.GasEscrow:
                    {
                        var gasEvent = nativeEvent.GetContent<GasEventData>();
                        var amount = TokenUtils.ToDecimal(gasEvent.amount, NativeTokenDecimals);
                        var price = TokenUtils.ToDecimal(gasEvent.price, NativeTokenDecimals);
                        return $"{amount} {PlatformName} tokens escrowed for contract gas, with price of {price} per gas unit";
                    }
                case EvtKind.GasPayment:
                    {
                        var gasEvent = nativeEvent.GetContent<GasEventData>();
                        var amount = TokenUtils.ToDecimal(gasEvent.amount, NativeTokenDecimals);
                        var price = TokenUtils.ToDecimal(gasEvent.price, NativeTokenDecimals);
                        return $"{amount} {PlatformName} tokens paid for contract gas, with price of {price} per gas unit";

                    }
                case EvtKind.TokenMint:
                case EvtKind.TokenBurn:
                case EvtKind.TokenSend:
                case EvtKind.TokenEscrow:
                case EvtKind.TokenReceive:
                    {
                        var data = Serialization.Unserialize<TokenEventData>(nativeEvent.Data);
                        var token = GetToken(data.symbol);
                        string action;

                        switch (evt.EvtKind)
                        {
                            case EvtKind.TokenMint: action = "minted"; break;
                            case EvtKind.TokenBurn: action = "burned"; break;
                            case EvtKind.TokenSend: action = "sent"; break;
                            case EvtKind.TokenReceive: action = "received"; break;
                            case EvtKind.TokenEscrow: action = "escrowed"; break;

                            default: action = "???"; break;
                        }

                        string chainText;

                        if (data.chainAddress.ToString() != block.ChainAddress)
                        {
                            Address srcAddress, dstAddress;

                            if (evt.EvtKind == EvtKind.TokenReceive)
                            {
                                srcAddress = data.chainAddress;
                                dstAddress = Address.FromText(block.ChainAddress);
                            }
                            else
                            {
                                srcAddress = Address.FromText(block.ChainAddress);
                                dstAddress = data.chainAddress;
                            }

                            chainText = $"from <a href=\"/chain/{srcAddress}\">{GetChainName(srcAddress.ToString())} chain</a> to <a href=\"/chain/{dstAddress}\">{GetChainName(dstAddress.ToString())} chain";
                        }
                        else
                        {
                            chainText = $"in <a href=\"/chain/{data.chainAddress}\">{GetChainName(data.chainAddress.ToString())} chain";
                        }

                        string fromAt = action == "sent" ? "from" : "at";
                        return $"{TokenUtils.ToDecimal(data.value, token.Decimals)} {token.Name} tokens {action} {fromAt} </a> address <a href=\"/address/{nativeEvent.Address}\">{nativeEvent.Address}</a> {chainText}.";
                    }

                default: return "Nothing.";
            }
        }

        public IEnumerable<AppDto> GetApps()
        {
            return Apps;
        }

        //TODO NEW without nexus

        private ChainDataAccess GetRootChain => _chains.FirstOrDefault(p =>
            p.Address.Equals(_rootChain.Address, StringComparison.InvariantCultureIgnoreCase) ||
            p.Name.Equals(_rootChain.Name, StringComparison.InvariantCultureIgnoreCase));

        private TokenDto GetNativeToken => _tokens["SOUL"];

        public IEnumerable<BlockDto> GetBlocks(string chainInput = null, int lastBlocksAmount = 20)
        {
            var blockList = new List<BlockDto>();

            // all chains
            if (string.IsNullOrEmpty(chainInput))
            {
                foreach (var chain in _chains)
                {
                    blockList.AddRange(chain.GetBlocks.Take((lastBlocksAmount / _chains.Count) + lastBlocksAmount)); //todo revisit logic
                }
                blockList = blockList.OrderByDescending(b => b.Timestamp).Take(lastBlocksAmount).ToList();
            }
            else //specific chain
            {
                var chain = GetChain(chainInput);
                if (chain != null)
                {
                    blockList.AddRange(chain.GetBlocks.Take(lastBlocksAmount));//don't need reorder
                }
            }
            return blockList;
        }

        public BlockDto GetBlock(string hash)
        {
            var blockHash = (Hash.Parse(hash));

            var block = FindBlockByHash(blockHash);
            return block;
        }

        public BlockDto GetBlock(int height, string chainAddress = null)
        {
            BlockDto block;

            if (string.IsNullOrEmpty(chainAddress)) // search in main chain
            {
                block = FindBlockByHeight(chainAddress, height);
            }
            else
            {
                var chain = GetChain(chainAddress);
                block = FindBlockByHeight(chain.Address, height);
            }

            if (block == null && !string.IsNullOrEmpty(chainAddress))
            {
                block = _phantasmaRpcService.GetBlockByHeight.SendRequestAsync(chainAddress, height).Result;
            }

            return block;
        }


        // aux methods
        public BlockDto FindBlockByHeight(string chainInput, int height)
        {
            var chain = GetChain(chainInput);
            return chain?.FindBlockByHeight(height);
        }

        public BlockDto FindBlockByHash(Hash hash)
        {
            foreach (var chain in _chains)
            {
                var block = chain.FindBlockByHash(hash);
                if (block != null)
                {
                    return block;
                }
            }

            return null;
        }

        public BlockDto FindBlockForTransaction(string txHash)
        {
            foreach (var chain in _chains)
            {
                foreach (var block in chain.GetBlocks)
                {
                    if (block.Txs.SingleOrDefault(p => p.Txid.Equals(txHash)) != null)
                    {
                        return block;
                    }
                }
            }

            return null;
        }

        public ChainDto GetChainInfoByAddress(string address)
        {
            return _chains.SingleOrDefault(p => p.Address.Equals(address))?.GetChainInfo;
        }

        public TransactionDto FindTransactionByHash(string hash)
        {
            foreach (var chain in _chains)
            {
                foreach (var block in chain.GetBlocks)
                {
                    var tx = block.Txs.SingleOrDefault(t => t.Txid.Equals(hash));
                    if (tx != null) return tx;
                }
            }

            return null;
        }

        public IEnumerable<ChainDto> GetAllChainsInfo()
        {
            return _chains.Select(p => p.GetChainInfo);
        }

        public IEnumerable<ChainDataAccess> GetAllChains()
        {
            return _chains;
        }

        public string GetChainName(string chainAddress)
        {
            return _chains.SingleOrDefault(p => p.Address == chainAddress)?.Name;
        }

        private ChainDataAccess SetupChain(ChainDto chain)
        {
            var newChain = new ChainDataAccess(chain);
            _chains.Add(newChain);
            return newChain;
        }

        private void SetupSingleBlock(ChainDataAccess chain, BlockDto block)
        {
            bool incrementCounter = false;
            foreach (var tx in block.Txs)
            {
                if (tx.Events != null && tx.Events.Any()) //todo not sure if this is needed
                {
                    TokenDto token = null;
                    foreach (var txEvent in tx.Events)
                    {
                        if (txEvent.Data != null)
                        {
                            var nativeEvent = new Event((EventKind)txEvent.EvtKind, //todo remove native event
                                Address.FromText((txEvent.EventAddress)), txEvent.Data.Decode());
                            Address address = Address.FromText((txEvent.EventAddress));
                            BigInteger amount;
                            TokenEventData data;

                            if (address != Address.Null)
                            {
                                AddAddressToList(address);
                            }

                            if (address != Address.Null)
                            {
                                chain.UpdateAddressTransactions(address, tx);
                            }

                            switch (txEvent.EvtKind)
                            {
                                case EvtKind.TokenBurn:
                                case EvtKind.TokenSend:
                                    data = nativeEvent.GetContent<TokenEventData>();
                                    token = GetToken(data.symbol);
                                    amount = data.value;
                                    address = nativeEvent.Address;
                                    if (token.Fungible)
                                    {
                                        chain.UpdateTokenBalance(token, address, amount, false);
                                    }
                                    else
                                    {
                                        chain.UpdateTokenOwnership(token, address, amount, false);
                                    }
                                    incrementCounter = true;
                                    break;

                                case EvtKind.TokenMint:
                                case EvtKind.TokenReceive:
                                    data = nativeEvent.GetContent<TokenEventData>();
                                    amount = data.value;
                                    address = nativeEvent.Address;
                                    token = GetToken(data.symbol);
                                    if (token.Fungible)
                                    {
                                        chain.UpdateTokenBalance(token, address, amount, true);
                                    }
                                    else
                                    {
                                        chain.UpdateTokenOwnership(token, address, amount, true);
                                    }
                                    incrementCounter = true;
                                    break;
                            }
                        }
                    }

                    if (incrementCounter && token != null)
                    {
                        UpdateTokenTransfer(token);
                    }
                }

                Debug.WriteLine($"setup block n:{block.Height}");
                chain.SetBlock(block);
            }
        }

        //todo move
        private async Task SetupBlocks(ChainDataAccess chain)
        {
            var height = await _phantasmaRpcService.GetBlockHeight.SendRequestAsync(chain.Address);

            for (int i = 1; i <= height; i++) //slooow
            {
                var blockDto = await _phantasmaRpcService.GetBlockByHeight.SendRequestAsync(chain.Address, i);
                SetupSingleBlock(chain, blockDto);
            }
        }

        private void AddAddressToList(Address address)
        {
            if (!_addresses.Contains(address))
            {
                _addresses.Add(address);
            }
        }

        private void UpdateTokenTransfer(TokenDto dto)
        {
            if (_tokenTransfers.ContainsKey(dto))
            {
                _tokenTransfers[dto] += 1;
            }
            else
            {
                _tokenTransfers.Add(dto, 1);
            }
        }
    }
}
