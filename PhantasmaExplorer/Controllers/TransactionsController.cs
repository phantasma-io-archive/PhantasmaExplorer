using System.Collections.Generic;
using Phantasma.Blockchain;
using Phantasma.Cryptography;
using Phantasma.Explorer.Infrastructure.Interfaces;
using Phantasma.Explorer.ViewModels;

namespace Phantasma.Explorer.Controllers
{
    public class TransactionsController
    {
        private IRepository Repository { get; } //todo interface

        public TransactionsController(IRepository repo)
        {
            Repository = repo;
        }

        public List<TransactionViewModel> GetLastTransactions()
        {
            var txList = new List<TransactionViewModel>();

            var repoTx = Repository.GetTransactions();
            foreach (var transaction in repoTx)
            {
                var block = Repository.NexusChain.FindBlockForTransaction(transaction);
                if (block != null)
                {
                    var tx1 = transaction;
                    txList.Add(TransactionViewModel.FromTransaction(Repository, BlockViewModel.FromBlock(Repository, block), tx1));
                }
            }
            return txList;
        }

        public TransactionViewModel GetTransaction(string txHash)
        {
            Transaction transaction = Repository.GetTransaction(txHash);
            if (transaction == null) return null;
            var block = Repository.NexusChain.FindBlockForTransaction(transaction);
            return TransactionViewModel.FromTransaction(Repository, BlockViewModel.FromBlock(Repository, block), transaction);
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
                var chain = Repository.NexusChain.FindChainForBlock(block);
                var transactions = chain.GetBlockTransactions(block);

                foreach (var transaction in transactions)
                {
                    var tx = transaction;
                    txList.Add(TransactionViewModel.FromTransaction(Repository, BlockViewModel.FromBlock(Repository, block), tx));
                }
            }

            return txList;
        }
    }
}
