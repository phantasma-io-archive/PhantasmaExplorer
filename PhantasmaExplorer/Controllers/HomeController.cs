using System.Collections.Generic;
using System.Linq;
using Phantasma.Explorer.Infrastructure.Interfaces;
using Phantasma.Explorer.ViewModels;

namespace Phantasma.Explorer.Controllers
{
    public class HomeController
    {
        public IRepository Repository { get; set; }

        public HomeController(IRepository repo)
        {
            Repository = repo;
        }

        public HomeViewModel GetLastestInfo()
        {
            var blocks = new List<BlockViewModel>();
            var txs = new List<TransactionViewModel>();
            foreach (var block in Repository.GetBlocks())
            {
                blocks.Add(BlockViewModel.FromBlock(block));
            }

            foreach (var transaction in Repository.GetTransactions())
            {
                var tempBlock = Repository.GetBlock(transaction);
                txs.Add(TransactionViewModel.FromTransaction(BlockViewModel.FromBlock(tempBlock), transaction, null));
            }

            var vm = new HomeViewModel
            {
                Blocks = blocks.OrderByDescending(b => b.Timestamp).ToList(),
                Transactions = txs
            };
            return vm;
        }
    }
}
