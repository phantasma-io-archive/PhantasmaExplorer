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
                if (transaction.Block != null)
                {
                    var tx1 = (Transaction)transaction;
                    txList.Add(TransactionViewModel.FromTransaction(Repository, BlockViewModel.FromBlock(Repository, transaction.Block), tx1));
                }
            }
            return txList;
        }

        public TransactionViewModel GetTransaction(string txHash)
        {
            Transaction transaction = Repository.GetTransaction(txHash);
            return TransactionViewModel.FromTransaction(Repository, BlockViewModel.FromBlock(Repository, transaction.Block), transaction);
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
                    txList.Add(TransactionViewModel.FromTransaction(Repository, BlockViewModel.FromBlock(Repository, block), tx));
                }
            }

            return txList;
        }
    }
}
