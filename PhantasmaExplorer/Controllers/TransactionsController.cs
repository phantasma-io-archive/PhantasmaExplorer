using System.Collections.Generic;
using System.Linq;
using Phantasma.Explorer.Application.Queries;
using Phantasma.Explorer.ViewModels;

namespace Phantasma.Explorer.Controllers
{
    public class TransactionsController
    {
        public List<TransactionViewModel> GetLastTransactions()
        {
            var txQuery = new TransactionQueries();

            var repoTx = txQuery.QueryTransactions();

            return repoTx.Select(TransactionViewModel.FromTransaction).ToList();
        }

        public TransactionViewModel GetTransaction(string txHash)
        {
            var txQuery = new TransactionQueries();
            var transaction = txQuery.QueryTransaction(txHash);

            if (transaction == null) return null;

            return TransactionViewModel.FromTransaction(transaction);
        }
    }
}
