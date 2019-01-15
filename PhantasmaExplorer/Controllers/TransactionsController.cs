using System.Collections.Generic;
using System.Linq;
using Phantasma.Explorer.Application.Queries;
using Phantasma.Explorer.Persistance;
using Phantasma.Explorer.ViewModels;

namespace Phantasma.Explorer.Controllers
{
    public class TransactionsController
    {
        private readonly ExplorerDbContext _context;

        public TransactionsController(ExplorerDbContext context)
        {
            _context = context;
        }

        public List<TransactionViewModel> GetLastTransactions()
        {
            var txQuery = new TransactionQueries(_context);

            var repoTx = txQuery.QueryTransactions();

            return repoTx.Select(TransactionViewModel.FromTransaction).ToList();
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
