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

            var repoTx = Repository.GetTransactions(txAmount: 20);
            foreach (var transaction in repoTx)
            {
                var block = Repository.GetBlock(transaction);
                if (block != null)
                {
                    var evts = new List<EventViewModel>();
                    var tx1 = (Transaction)transaction;
                    foreach (var evt in tx1.Events)
                    {
                        evts.Add(new EventViewModel()
                        {
                            Kind = evt.Kind,
                            Content = Repository.GetEventContent(block, evt)
                        });
                    }
                    txList.Add(TransactionViewModel.FromTransaction(Repository.NexusChain,BlockViewModel.FromBlock(block), tx1, evts));
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
                    Content = Repository.GetEventContent(block, evt)
                });
            }
            return TransactionViewModel.FromTransaction(Repository.NexusChain, BlockViewModel.FromBlock(block), transaction, evts);
        }

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
                            Content = Repository.GetEventContent(block, evt)
                        });
                    }
                    txList.Add(TransactionViewModel.FromTransaction(Repository.NexusChain, BlockViewModel.FromBlock(block), tx, evts));
                }
            }

            return txList;
        }
    }
}
