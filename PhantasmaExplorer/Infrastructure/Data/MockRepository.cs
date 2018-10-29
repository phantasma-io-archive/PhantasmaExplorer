using System.Collections.Generic;
using System.Linq;
using Phantasma.Blockchain;
using Phantasma.Blockchain.Contracts;
using Phantasma.Blockchain.Contracts.Native;
using Phantasma.Blockchain.Plugins;
using Phantasma.Blockchain.Tokens;
using Phantasma.Cryptography;
using Phantasma.Explorer.Infrastructure.Interfaces;
using Phantasma.IO;

namespace Phantasma.Explorer.Infrastructure.Data
{
    public class MockRepository : IRepository
    {
        public Nexus NexusChain { get; set; }

        public decimal GetAddressBalance(Address address, string chainName = null) //todo this should not be here
        {
            if (string.IsNullOrEmpty(chainName))
            {
                return TokenUtils.ToDecimal(NexusChain.RootChain.GetTokenBalance(NexusChain.NativeToken, address), NexusChain.NativeToken.Decimals);
            }

            var chain = GetChainByName(chainName);
            return TokenUtils.ToDecimal(chain?.GetTokenBalance(NexusChain.NativeToken, address), NexusChain.NativeToken.Decimals);
        }

        public IEnumerable<Address> GetAddressList(string chainAddress = null, int count = 20) //todo strategy to get address
        {
            // if chainAddress then look only in a certain chain
            // count number of address to return

            var plugin = NexusChain.GetPlugin<ChainAddressesPlugin>();
            if (plugin == null)
            {
                return Enumerable.Empty<Address>();
            }

            if (string.IsNullOrEmpty(chainAddress))
            {
                var addressList = new List<Address>();
                foreach (var chain in NexusChain.Chains)
                {
                    addressList.AddRange(plugin.GetChainAddresses(chain));
                }
                return addressList.Take(100);
            }
            else
            {
                var chain = NexusChain.FindChainByAddress(Address.FromText(chainAddress));
                return plugin.GetChainAddresses(chain);
            }
        }

        public Address GetAddress(string addressText) //todo
        {
            var address = Address.FromText(addressText);
            return address;
        }

        public IEnumerable<Block> GetBlocks(string chainInput = null, int lastBlocksAmount = 20)
        {
            var blockList = new List<Block>();

            // all chains
            if (string.IsNullOrEmpty(chainInput))
            {
                foreach (var chain in NexusChain.Chains)
                {
                    blockList.AddRange(chain.Blocks.Take(10));
                }
                blockList = blockList.Take(lastBlocksAmount).ToList();
            }
            else //specific chain
            {
                var chain = GetChain(chainInput);
                if (chain != null && chain.Blocks.Any())
                {
                    blockList.AddRange(chain.Blocks.Take(lastBlocksAmount));
                }
            }
            return blockList;
        }

        public Block GetBlock(string hash)
        {
            var blockHash = (Hash.Parse(hash));
            foreach (var chain in NexusChain.Chains)
            {
                var block = chain.FindBlockByHash(blockHash);
                if (block != null)
                {
                    return block;
                }
            }

            return null;
        }

        public Block GetBlock(int height, string chainAddress = null)
        {
            Block block;

            if (string.IsNullOrEmpty(chainAddress)) // search in main chain
            {
                block = NexusChain.RootChain.FindBlockByHeight(height);
            }
            else
            {
                var chain = GetChain(chainAddress);
                block = chain.FindBlockByHeight(height);
            }

            return block;
        }

        public IEnumerable<Chain> GetAllChains()
        {
            return NexusChain.Chains.ToList();
        }

        public Chain GetChain(string chainInput)
        {
            if (!Address.IsValidAddress(chainInput)) return null;
            var chainAddress = Address.FromText(chainInput);

            return NexusChain.Chains.SingleOrDefault(c => c.Address == chainAddress);
        }

        public Chain GetChainByName(string chainName)
        {
            return NexusChain.Chains.SingleOrDefault(c => c.Name == chainName);
        }

        public IEnumerable<string> GetChainNames()
        {
            var nameList = new List<string>();
            foreach (var chain in GetAllChains())
            {
                nameList.Add(chain.Name);
            }

            return nameList;
        }

        public IEnumerable<Transaction> GetTransactions(string chainAddress, int txAmount)
        {
            var txList = new List<Transaction>();
            var blocksList = new List<Block>();
            if (string.IsNullOrEmpty(chainAddress)) //all chains
            {
                var chains = GetAllChains();
                foreach (var chain in chains)
                {
                    blocksList.AddRange(chain.Blocks.Take(txAmount * 10));
                }

                foreach (var block in blocksList)
                {
                    foreach (var tx in block.Transactions)
                    {
                        txList.Add((Transaction)tx);
                        if (txList.Count == txAmount) return txList;
                    }
                }
            }
            else
            {
                var chain = GetChain(chainAddress);
                if (chain != null)
                {
                    foreach (var block in chain.Blocks)
                    {
                        foreach (var tx in block.Transactions)
                        {
                            txList.Add((Transaction)tx);
                            if (txList.Count == txAmount) return txList;
                        }
                    }
                }
            }

            return txList;
        }

        public Transaction GetTransaction(string txHash)
        {
            var hash = Hash.Parse(txHash);
            foreach (var chain in NexusChain.Chains)
            {
                var tx = chain.FindTransactionByHash(hash);
                if (tx != null)
                {
                    return tx;
                }
            }
            return null;
        }

        public int GetTotalTransactions()
        {
            return NexusChain.GetTotalTransactionCount();
        }

        public IEnumerable<Token> GetTokens()
        {
            return NexusChain.Tokens.ToList();
        }

        public Token GetToken(string symbol)
        {
            return NexusChain.Tokens.SingleOrDefault(t => t.Symbol.ToUpperInvariant() == symbol || t.Name.ToUpperInvariant() == symbol);
        }

        public string GetEventContent(Block block, Event evt)
        {
            switch (evt.Kind)
            {
                case EventKind.ChainCreate:
                    {
                        var chainAddress = Serialization.Unserialize<Address>(evt.Data);
                        var chain = NexusChain.FindChainByAddress(chainAddress);
                        return $"{chain.Name} chain created at address <a href=\"/chain/{chainAddress}\">{chainAddress}</a>.";
                    }

                case EventKind.TokenCreate:
                    {
                        var symbol = Serialization.Unserialize<string>(evt.Data);
                        var token = NexusChain.FindTokenBySymbol(symbol);
                        return $"{token.Name} token created with symbol <a href=\"/token/{symbol}\">{symbol}</a>.";
                    }

                case EventKind.TokenMint:
                case EventKind.TokenBurn:
                case EventKind.TokenSend:
                case EventKind.TokenReceive:
                    {
                        var data = Serialization.Unserialize<TokenEventData>(evt.Data);
                        var token = NexusChain.FindTokenBySymbol(data.symbol);
                        string action;

                        switch (evt.Kind)
                        {
                            case EventKind.TokenMint: action = "minted"; break;
                            case EventKind.TokenBurn: action = "burned"; break;
                            case EventKind.TokenSend: action = "sent"; break;
                            case EventKind.TokenReceive: action = "received"; break;
                            default: action = "???"; break;
                        }

                        string chainText;

                        if (data.chainAddress != block.Chain.Address)
                        {
                            Address srcAddress, dstAddress;

                            if (evt.Kind == EventKind.TokenReceive)
                            {
                                srcAddress = data.chainAddress;
                                dstAddress = block.Chain.Address;
                            }
                            else
                            {
                                srcAddress = block.Chain.Address;
                                dstAddress = data.chainAddress;
                            }

                            chainText = $"from <a href=\"/chain/{srcAddress}\">{GetChainName(NexusChain, srcAddress)} chain</a> to <a href=\"/chain/{dstAddress}\">{GetChainName(NexusChain, dstAddress)} chain";
                        }
                        else
                        {
                            chainText = $"in <a href=\"/chain/{data.chainAddress}\">{GetChainName(NexusChain, data.chainAddress)} chain";
                        }

                        return $"{TokenUtils.ToDecimal(data.amount, token.Decimals)} {token.Name} tokens {action} at </a> address <a href=\"/address/{evt.Address}\">{evt.Address}</a> {chainText}.";
                    }

                default: return "Nothing.";
            }
        }

        private static string GetChainName(Nexus nexus, Address chainAddress)
        {
            var chain = nexus.FindChainByAddress(chainAddress);
            if (chain != null)
            {
                return chain.Name;
            }

            return "???";
        }

    }
}
