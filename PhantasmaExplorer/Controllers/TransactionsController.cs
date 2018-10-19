using System.Collections.Generic;
using System.Linq;
using Phantasma.Blockchain;
using Phantasma.Cryptography;
using Phantasma.Explorer.Infrastructure.Interfaces;
using Phantasma.Explorer.ViewModels;

namespace Phantasma.Explorer.Controllers
{
    public class TransactionsController
    {
        public IRepository Repository { get; set; } //todo interface

        public TransactionsController(IRepository repo)
        {
            Repository = repo;
        }

        public List<TransactionViewModel> GetLastTransactions()
        {
            var repoChains = Repository.GetAllChains();
            var txList = new List<TransactionViewModel>();
            foreach (var chain in repoChains)
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
                                Content = Repository.GetEventContent(block, evt), //todo fix me
                            });
                        }

                        txList.Add(TransactionViewModel.FromTransaction(BlockViewModel.FromBlock(block), tx1, evts));
                    }
                }
            }
            return txList;
        }

        public TransactionViewModel GetTransaction(string txHash)
        {
            Transaction transaction = Repository.GetTransaction(txHash);

            Block block = Repository.GetBlock(transaction);
            var evts = new List<EventViewModel>();
            foreach (var evt in transaction.Events)
            {
                evts.Add(new EventViewModel()
                {
                    Kind = evt.Kind,
                    Content = Repository.GetEventContent(block, evt), //todo fix me
                });
            }
            return TransactionViewModel.FromTransaction(BlockViewModel.FromBlock(block), transaction, evts);
        }

        // todo tomorrow
        public List<TransactionViewModel> GetTransactionsByBlock(string input)
        {
            var blockHash = Hash.Parse(input);

            Block block = null;
            var txList = new List<TransactionViewModel>();
            var chains = Repository.GetAllChains();

            foreach (var chain in chains)
            {
                var x = chain.FindBlockByHash(blockHash);
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
                            Content = Repository.GetEventContent(block, evt), //todo fix me
                        });
                    }
                    txList.Add(TransactionViewModel.FromTransaction(BlockViewModel.FromBlock(block), tx, evts));
                }
            }

            return txList;
        }
    }
}
