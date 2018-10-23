using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Phantasma.Cryptography;
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

            var command = new Task(() =>
             {
                 var x = 1;
             });

            var vm = new HomeViewModel
            {
                Blocks = blocks.OrderByDescending(b => b.Timestamp).ToList(),
                Transactions = txs,
                SearchCommand = command
            };
            return vm;
        }

        public string SearchCommand(string input)
        {
            try
            {
                var token = Repository.GetToken(input);
                if (token != null)// token symbol
                {
                    return $"token/{input}";
                }
                if (input.Length == 45) //maybe is address
                {
                    var address = Address.FromText(input);
                    return $"address/{input}";
                }

                var hash = Hash.Parse(input);
                if (hash != null)
                {
                    var tx = Repository.GetTransaction(hash.ToString());
                    if (tx != null)
                    {
                        return $"tx/{input}";
                    }

                    var block = Repository.GetBlock(hash.ToString());
                    if (block != null)
                    {
                        return $"block/{input}";
                    }
                }

                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return "/home";
            }
        }
    }
}
