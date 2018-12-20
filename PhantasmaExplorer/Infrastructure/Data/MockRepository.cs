using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Phantasma.Blockchain;
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
    public class MockRepository : IRepository //todo getTokenOwnserships
    {
        //public Nexus NexusChain { get; set; }

        private RootChainDto _rootChain;
        private List<ChainDataAccess> _chains = new List<ChainDataAccess>();
        private Dictionary<string, TokenDto> _tokens = new Dictionary<string, TokenDto>();
        private int NativeTokenDecimals = 8;

        public List<AppDto> Apps { get; set; }

        private IPhantasmaRpcService _phantasmaRpcService;

        public async Task InitRepo()
        {
            _phantasmaRpcService = new PhantasmaRpcService(new RpcClient.Client.RpcClient(new Uri("http://localhost:7077/rpc")));

            var root = await _phantasmaRpcService.GetRootChain.SendRequestAsync();
            var chains = await _phantasmaRpcService.GetChains.SendRequestAsync(); //name-address info only
            var tokens = await _phantasmaRpcService.GetTokens.SendRequestAsync();
            var appList = await _phantasmaRpcService.GetApplications.SendRequestAsync();

            Apps = appList.Apps;
            _rootChain = root;

            foreach (var token in tokens.Tokens)
            {
                _tokens.Add(token.Symbol, token);
            }

            // working

            foreach (var chain in chains)
            {
                var blocks = new Dictionary<Hash, BlockDto>();
                var height = await _phantasmaRpcService.GetBlockHeight.SendRequestAsync(chain.Address);
                for (int i = 1; i <= height; i++)//slooow
                {
                    var blockDto = await _phantasmaRpcService.GetBlockByHeight.SendRequestAsync(root.Name, i);
                    blocks.Add(Hash.Parse(blockDto.Hash), blockDto);
                }
                _chains.Add(new ChainDataAccess(chain, blocks));
            }

            var testGetBlocks1 = GetBlocks(); // works
            var testGetBlocks2 = GetBlocks("main", 10); //works
        }

        public decimal GetAddressNativeBalance(Address address, string chainName = null) //todo this should not be here
        {
            //todo
            //if (string.IsNullOrEmpty(chainName))
            //{
            //    return TokenUtils.ToDecimal(NexusChain.RootChain.GetTokenBalance(NexusChain.NativeToken, address), NexusChain.NativeToken.Decimals);
            //}

            //var chain = GetChainName(chainName);
            return 0;
            //return TokenUtils.ToDecimal(chain?.GetTokenBalance(NexusChain.NativeToken, address), NexusChain.NativeToken.Decimals);
        }

        public decimal GetAddressBalance(Address address, TokenDto token, string chainName)
        {
            return 0;//todo
        }

        public decimal GetAddressBalance(Address address, Token token, string chainName)
        {
            return 0;
            //todo
            //var chain = GetChainByName(chainName);
            //decimal balance = 0;
            //if (chain != null)
            //{
            //    balance = TokenUtils.ToDecimal(chain.GetTokenBalance(token, address), token.Decimals);
            //}

            //return balance;
        }

        public IEnumerable<Address> GetAddressList(string chainAddress = null, int count = 20) //todo strategy to get address
        {
            // if chainAddress then look only in a certain chain
            // count number of address to return
            return null;
            //var plugin = NexusChain.GetPlugin<ChainAddressesPlugin>();
            //if (plugin == null)
            //{
            //    return Enumerable.Empty<Address>();
            //}

            //if (string.IsNullOrEmpty(chainAddress))
            //{
            //    var addressList = new List<Address>();
            //    foreach (var chain in NexusChain.Chains)
            //    {
            //        addressList.AddRange(plugin.GetChainAddresses(chain));
            //    }
            //    return addressList.Take(100);
            //}
            //else
            //{
            //    var chain = NexusChain.GetChainInfoByAddress(Address.FromText(chainAddress));
            //    return plugin.GetChainAddresses(chain);
            //}
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
            //    var plugin = NexusChain.GetPlugin<AddressTransactionsPlugin>();
            //    return plugin?.GetAddressTransactions(address).OrderByDescending(tx => NexusChain.FindBlockForTransaction(tx).Timestamp.Value).Take(amount);
            return null; //todo
        }

        public int GetAddressTransactionCount(Address address, string chainName)
        {
            //todo
            //var chain = NexusChain.FindChainByName(chainName);

            //var plugin = NexusChain.GetPlugin<AddressTransactionsPlugin>();
            //if (plugin != null)
            //{
            //    return plugin.GetAddressTransactions(address).Count(tx => NexusChain.FindBlockForTransaction(tx).ChainAddress == chain.Address);
            //}

            return 0;
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
                total += blocks.Select(p => p.Txs).Count();
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

        public IEnumerable<TransactionDto> GetLastTokenTransfers(string symbol, int amount)
        {
            //todo
            //var token = GetToken(symbol);
            //var plugin = NexusChain.GetPlugin<TokenTransactionsPlugin>();

            //if (token != null && plugin != null)
            //{
            //    return plugin.GetTokenTransactions(token).OrderByDescending(tx => NexusChain.FindBlockForTransaction(tx).Timestamp.Value).Take(amount);
            //}

            return null;
        }

        public int GetTokenTransfersCount(string symbol)
        {
            //todo
            //var token = GetToken(symbol);
            //var plugin = NexusChain.GetPlugin<TokenTransactionsPlugin>();
            //if (token != null && plugin != null)
            //{
            //    return plugin.GetTokenTransactions(token).Count();
            //}

            return 0;
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
                        return $"{amount} {Nexus.PlatformName} tokens escrowed for contract gas, with price of {price} per gas unit";
                    }
                case EvtKind.GasPayment:
                    {
                        var gasEvent = nativeEvent.GetContent<GasEventData>();
                        var amount = TokenUtils.ToDecimal(gasEvent.amount, NativeTokenDecimals);
                        var price = TokenUtils.ToDecimal(gasEvent.price, NativeTokenDecimals);
                        return $"{amount} {Nexus.PlatformName} tokens paid for contract gas, with price of {price} per gas unit";

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
            if (block != null)
            {
                return block;
            }

            //try get by rpc
            var blockDto = _phantasmaRpcService.GetBlockByHash.SendRequestAsync(hash).Result; //todo remove
            return blockDto;
        }

        public BlockDto GetBlock(int height, string chainAddress = null)
        {
            BlockDto block = null;

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
                    if (block.Txs.SingleOrDefault(p => p.Txid == txHash) != null)
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
                    return block.Txs.Find(t => t.Txid == hash);
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
    }
}
