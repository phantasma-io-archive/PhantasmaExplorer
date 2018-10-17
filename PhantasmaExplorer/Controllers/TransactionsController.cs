using System.Collections.Generic;
using System.Linq;
using Phantasma.Blockchain;
using Phantasma.Blockchain.Contracts;
using Phantasma.Blockchain.Contracts.Native;
using Phantasma.Cryptography;
using Phantasma.Explorer.ViewModels;
using Phantasma.IO;

namespace Phantasma.Explorer.Controllers
{
    public class TransactionsController
    {
        public Nexus NexusChain { get; set; } //todo this should be replace with a repository or db instance

        public TransactionsController(Nexus chain)
        {
            NexusChain = chain;
        }

        public List<TransactionViewModel> GetTransactions()
        {
            var txList = new List<TransactionViewModel>();

            foreach (var chain in NexusChain.Chains)
            {
                foreach (var block in chain.Blocks.TakeLast(20))
                {
                    foreach (var tx in block.Transactions)
                    {
                        var evts = new List<EventViewModel>();
                        var tx1 = (Transaction)tx;
                        foreach (var evt in tx1.Events)
                        {
                            evts.Add(new EventViewModel()
                            {
                                Kind = evt.Kind,
                                Content = GetEventContent(NexusChain, block, evt), //todo fix me
                            });
                        }
                        txList.Add(TransactionViewModel.FromTransaction(BlockViewModel.FromBlock(block), (Transaction)tx, evts));
                    }
                }
            }
            return txList;
        }

        public TransactionViewModel GetTransaction(string txHash)
        {
            var hash = Hash.Parse(txHash);
            Transaction tx = null;
            Chain targetChain = null;
            foreach (var chain in NexusChain.Chains)
            {
                tx = chain.FindTransaction(hash);
                if (tx != null)
                {
                    targetChain = chain;
                    break;
                }
            }

            if (targetChain != null)
            {
                var block = targetChain.FindTransactionBlock(tx);
                var evts = new List<EventViewModel>();
                foreach (var evt in tx.Events)
                {
                    evts.Add(new EventViewModel()
                    {
                        Kind = evt.Kind,
                        Content = GetEventContent(NexusChain, block, evt), //todo fix me
                    });
                }
                return TransactionViewModel.FromTransaction(BlockViewModel.FromBlock(block), tx, evts);
            }

            return null;
        }

        public List<TransactionViewModel> GetTransactionsByBlock(string input)
        {
            var blockHash = Hash.Parse(input);
            Block block = null;
            var txList = new List<TransactionViewModel>();

            foreach (var chain in NexusChain.Chains)
            {
                var x = chain.FindBlock(blockHash);
                if (x != null)
                {
                    block = x;
                    break;
                }
            }

            if (block != null)
            {
                foreach (var transaction in block.Transactions)
                {
                    var tx = (Transaction)transaction;
                    var evts = new List<EventViewModel>();
                    foreach (var evt in tx.Events)
                    {
                        evts.Add(new EventViewModel()
                        {
                            Kind = evt.Kind,
                            Content = GetEventContent(NexusChain, block, evt), //todo fix me
                        });
                    }
                    txList.Add(TransactionViewModel.FromTransaction(BlockViewModel.FromBlock(block), tx, evts));
                }
            }

            return txList;
        }


        // TODO exception and error handling and move this
        public static string GetEventContent(Nexus nexus, Block block, Event evt)
        {
            switch (evt.Kind)
            {
                case EventKind.ChainCreate:
                    {
                        var chainAddress = Serialization.Unserialize<Address>(evt.Data);
                        var chain = nexus.FindChainByAddress(chainAddress);
                        return $"{chain.Name} chain created at address <a href=\"/chain/{chainAddress}\">{chainAddress}</a>.";
                    }

                case EventKind.TokenCreate:
                    {
                        var symbol = Serialization.Unserialize<string>(evt.Data);
                        var token = nexus.FindTokenBySymbol(symbol);
                        return $"{token.Name} token created with symbol <a href=\"/token/{symbol}\">{symbol}</a>.";
                    }

                case EventKind.TokenMint:
                case EventKind.TokenBurn:
                case EventKind.TokenSend:
                case EventKind.TokenReceive:
                    {
                        var data = Serialization.Unserialize<TokenEventData>(evt.Data);
                        var token = nexus.FindTokenBySymbol(data.symbol);
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

                            chainText = $"from <a href=\"/chain/{srcAddress}\">{GetChainName(nexus, srcAddress)} chain</a> to <a href=\"/chain/{dstAddress}\">{GetChainName(nexus, dstAddress)} chain";
                        }
                        else
                        {
                            chainText = $"in <a href=\"/chain/{data.chainAddress}\">{GetChainName(nexus, data.chainAddress)} chain";
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
