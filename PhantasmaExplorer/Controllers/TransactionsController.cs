using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Phantasma.Explorer.Application.Queries;
using Phantasma.Explorer.Persistance;
using Phantasma.Explorer.ViewModels;

namespace Phantasma.Explorer.Controllers
{
    public class TransactionsController
    {
        public List<TransactionViewModel> GetLastTransactions()
        {
            var context = Explorer.AppServices.GetService<ExplorerDbContext>();

            var txQuery = new TransactionQueries(context);

            var repoTx = txQuery.QueryTransactions();

            return repoTx.Select(TransactionViewModel.FromTransaction).ToList();
        }

        public TransactionViewModel GetTransaction(string txHash)
        {
            var context = Explorer.AppServices.GetService<ExplorerDbContext>();

            var txQuery = new TransactionQueries(context);
            var transaction = txQuery.QueryTransaction(txHash);

            if (transaction == null) return null;

            return TransactionViewModel.FromTransaction(transaction);
        }
    }
}
