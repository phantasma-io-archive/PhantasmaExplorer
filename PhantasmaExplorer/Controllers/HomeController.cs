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

            var chart = new Dictionary<string, uint>();

            foreach (var transaction in Repository.GetTransactions())
            {
                var tempBlock = Repository.GetBlock(transaction);
                txs.Add(TransactionViewModel.FromTransaction(BlockViewModel.FromBlock(tempBlock), transaction, null));
            }

            // tx history chart calculation
            var repTxs = Repository.GetTransactions(null, 1000);
            foreach (var transaction in repTxs)
            {
                var tempBlock = Repository.GetBlock(transaction);

                DateTime chartTime = tempBlock.Timestamp;
                var chartKey = $"{chartTime.Day}/{chartTime.Month}";

                if (chart.ContainsKey(chartKey))
                {
                    chart[chartKey]+=200;
                }
                else
                {
                    chart[chartKey] = 1;
                }
            }

            var command = new Task(() =>
             {
                 var x = 1;
             });

            var vm = new HomeViewModel
            {
                Blocks = blocks.OrderByDescending(b => b.Timestamp).ToList(),
                Transactions = txs,
                Chart = chart,
                SearchCommand = command
            };
            return vm;
        }

        public string SearchCommand(string input)
        {
            try
            {
                if (Address.IsValidAddress(input)) //maybe is address
                {
                    return $"address/{input}";
                }

                var token = Repository.GetToken(input.ToUpperInvariant());
                if (token != null)// token
                {
                    return $"token/{token.Symbol}";
                }

                var chain = Repository.GetChainByName(input) ?? Repository.GetChain(input);
                if (chain != null)
                {
                    return $"chain/{chain.Address.Text}";
                }

                var hash = Hash.Parse(input);
                if (hash != null)
                {
                    var tx = Repository.GetTransaction(hash.ToString());
                    if (tx != null)
                    {
                        return $"tx/{tx.Hash}";
                    }

                    var block = Repository.GetBlock(hash.ToString());
                    if (block != null)
                    {
                        return $"block/{block.Hash}";
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
