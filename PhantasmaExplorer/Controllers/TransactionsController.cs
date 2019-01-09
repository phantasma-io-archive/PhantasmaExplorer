using System.Collections.Generic;
using Phantasma.Cryptography;
using Phantasma.Explorer.Infrastructure.Interfaces;
using Phantasma.Explorer.ViewModels;
using Phantasma.RpcClient.DTOs;

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
                var block = Repository.FindBlockForTransaction(transaction.Txid);
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
            var transaction = Repository.GetTransaction(txHash);
            if (transaction == null) return null;
            var block = Repository.FindBlockForTransaction(transaction.Txid);
            return TransactionViewModel.FromTransaction(Repository, BlockViewModel.FromBlock(Repository, block), transaction);
        }

        public List<TransactionViewModel> GetTransactionsByBlock(string input)
        {
            var blockHash = Hash.Parse(input);

            BlockDto block = null;
            var txList = new List<TransactionViewModel>();

            block = Repository.GetBlock(blockHash.ToString());

            if (block != null)
            {
                foreach (var transaction in block.Txs)
                {
                    var tx = transaction;
                    txList.Add(TransactionViewModel.FromTransaction(Repository, BlockViewModel.FromBlock(Repository, block), tx));
                }
            }

            return txList;
        }
    }
}
