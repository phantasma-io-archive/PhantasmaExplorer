using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Phantasma.Explorer.Application;
using Phantasma.Explorer.Application.Queries;
using Phantasma.Explorer.Persistance;
using Phantasma.Explorer.ViewModels;

namespace Phantasma.Explorer.Controllers
{
    public class TransactionsController : BaseController
    {
        public TransactionsController() : base(Explorer.AppServices.GetService<ExplorerDbContext>()) { }

        public int GetTransactionsCount(string chain = null)
        {
            var txQuery = new TransactionQueries(_context);

            return txQuery.QueryTotalChainTransactionCount(chain);
        }

        public List<TransactionViewModel> GetTransactions(int currentPage, int pageSize = AppSettings.PageSize, string chain = null)
        {
            var txQuery = new TransactionQueries(_context);

            var query = txQuery.QueryTransactions(chain).Skip((currentPage - 1) * pageSize).Take(pageSize);

            return query.AsEnumerable().Select(TransactionViewModel.FromTransaction).ToList();
        }

        public TransactionViewModel GetTransaction(string txHash)
        {
            var txQuery = new TransactionQueries(_context);
            var transaction = txQuery.QueryTransaction(txHash);

            if (transaction == null) return null;

            return TransactionViewModel.FromTransaction(transaction);
        }
    }
}
