﻿using System.Collections.Generic;
using System.Linq;
using Phantasma.Blockchain;
using Phantasma.Blockchain.Contracts;
using Phantasma.Blockchain.Contracts.Native;
using Phantasma.Blockchain.Tokens;
using Phantasma.Cryptography;
using Phantasma.Explorer.Infrastructure.Interfaces;
using Phantasma.IO;

namespace Phantasma.Explorer.Infrastructure.Data
{
    public class MockRepository : IRepository
    {
        public Nexus NexusChain { get; set; }

        public decimal GetAddressBalance(Address address) //todo this should not be here
        {
            return TokenUtils.ToDecimal(NexusChain.RootChain.GetTokenBalance(NexusChain.NativeToken, address));
        }

        public List<Address> GetAddressList(string chainAddress = "", int count = 20) //todo strategy to get address
        {
            // if chainAddress then look only in a certain chain
            // count number of address to return

            var addressList = new List<Address>();

            var targetAddress = Address.FromText("PGasVpbFYdu7qERihCsR22nTDQp1JwVAjfuJ38T8NtrCB"); //todo remove hack
            var ownerKey = KeyPair.FromWIF("L2G1vuxtVRPvC6uZ1ZL8i7Dbqxk9VPXZMGvZu9C3LXpxKK51x41N");
            addressList.Add(ownerKey.Address);
            addressList.Add(targetAddress);

            return addressList;
        }

        public Address GetAddress(string addressText) //todo
        {
            var address = Address.FromText(addressText);
            return address;
        }

        public List<Block> GetBlocks(string chainInput = "", int lastBlocksAmount = 20)
        {
            List<Block> blockList = new List<Block>();

            // all chains
            if (string.IsNullOrEmpty(chainInput))
            {
                foreach (var chain in NexusChain.Chains)
                {
                    if (chain.Blocks.Any())
                    {
                        blockList.AddRange(chain.Blocks.TakeLast(lastBlocksAmount));
                    }
                }
            }
            else //specific chain
            {
                var chain = GetChain(chainInput);
                if (chain != null && chain.Blocks.Any())
                {
                    blockList.AddRange(chain.Blocks.TakeLast(lastBlocksAmount));
                }
            }
            blockList = blockList.OrderBy(block => block.Timestamp.Value).ToList();

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

        public Block GetBlock(int height, string chainAddress = "")
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

        public List<Chain> GetAllChains()
        {
            return NexusChain.Chains.ToList();
        }

        public Chain GetChain(string chainInput)
        {
            var chainAddress = Address.FromText(chainInput);

            return NexusChain.Chains.SingleOrDefault(c => c.Address == chainAddress);
        }

        public List<Transaction> GetTransactions(string chainAddress = "", int txAmount = 20) //todo turns out this does not have use yet -.-
        {
            var txList = new List<Transaction>();
            if (string.IsNullOrEmpty(chainAddress)) //all chains
            {
                var chains = GetAllChains();
                foreach (var chain in chains)
                {
                    foreach (var block in chain.Blocks.TakeLast(txAmount))
                    {
                        foreach (var tx in block.Transactions)
                        {                           
                            txList.Add((Transaction)tx);
                        }
                    }
                }
            }
            else
            {
                var chain = GetChain(chainAddress);
                if (chain != null)
                {
                    foreach (var block in chain.Blocks.TakeLast(txAmount))
                    {
                        foreach (var tx in block.Transactions)
                        {
                            txList.Add((Transaction)tx);
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
                var tx = chain.FindTransaction(hash);
                if (tx != null)
                {
                    return tx;
                }
            }
            return null;
        }

        public Block GetBlock(Transaction tx)
        {
            var chains = GetAllChains();
            Chain targetChain = null;
            foreach (var chain in chains)//todo redo this stupid thing xD
            {
                var transaction = chain.FindTransaction(tx.Hash);
                if (transaction != null)
                {
                    targetChain = chain;
                    break;
                }
            }

            return targetChain?.FindTransactionBlock(tx);
        }

        public List<Token> GetTokens()
        {
            return NexusChain.Tokens.ToList();
        }

        public Token GetToken(string symbol)
        {
            return NexusChain.Tokens.SingleOrDefault(t => t.Symbol == symbol);
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

                        return $"{TokenUtils.ToDecimal(data.amount)} {token.Name} tokens {action} at </a> address <a href=\"/address/{evt.Address}\">{evt.Address}</a> {chainText}.";
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