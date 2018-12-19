using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Phantasma.Cryptography;
using Phantasma.Explorer.Infrastructure.Interfaces;
using Phantasma.Explorer.Utils;
using Phantasma.Explorer.ViewModels;

namespace Phantasma.Explorer.Controllers
{
    public class HomeController
    {
        private IRepository Repository { get; }

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
                blocks.Add(BlockViewModel.FromBlock(Repository, block));
            }

            var chart = new Dictionary<string, uint>();

            foreach (var transaction in Repository.GetTransactions())
            {
                var block = Repository.FindBlockForTransaction(transaction.Txid);
                txs.Add(TransactionViewModel.FromTransaction(Repository, BlockViewModel.FromBlock(Repository, block), transaction));
            }

            // tx history chart calculation
            var repTxs = Repository.GetTransactions(null, 1000);
            foreach (var transaction in repTxs)
            {
                var block = Repository.FindBlockForTransaction(transaction.Txid);

                DateTime chartTime = DateTime.Parse(block.Timestamp);
                var chartKey = $"{chartTime.Day}/{chartTime.Month}";

                if (chart.ContainsKey(chartKey))
                {
                    chart[chartKey] += 200;
                }
                else
                {
                    chart[chartKey] = 1;
                }
            }

            int totalChains = Repository.GetChainCount();
            int height = Repository.GetChain("main").ChainInfo.Height; //todo repo
            int totalTransactions = Repository.GetTotalTransactions();

            var vm = new HomeViewModel
            {
                Blocks = blocks,
                Transactions = txs,
                Chart = chart,
                TotalTransactions = totalTransactions,
                TotalChains = totalChains,
                BlockHeight = height,
            };
            return vm;
        }

        public List<CoinRateViewModel> GetRateInfo()
        {
            var symbols = new[] { "USD", "BTC", "ETH", "NEO" };

            var tasks = symbols.Select(symbol => CoinUtils.GetCoinInfoAsync(CoinUtils.SoulId, symbol));
            var rates = Task.WhenAll(tasks).GetAwaiter().GetResult();

            int days = 15;
            var soulData = CoinUtils.GetChartForCoin("SOUL*", "USD", days);

            var coins = new List<CoinRateViewModel>();
            for (int i = 0; i < rates.Length; i++)
            {
                var symbol = symbols[i];

                var historicalData = symbol == "USD" ? null : CoinUtils.GetChartForCoin(symbol, "USD", days);

                var chart = new Dictionary<string, decimal>();
                for (int day=0; day<days; day++)
                {
                    DateTime date = DateTime.Now - TimeSpan.FromDays(day);
                    var chartKey = $"{date.Day}/{date.Month}";

                    decimal price;
                    if (historicalData == null)
                    {
                        price = soulData[day];
                    }
                    else
                    {
                        price = soulData[day] / historicalData[day];
                    }

                    chart[chartKey] = price;
                }

                var coin = new CoinRateViewModel
                {
                    Symbol = symbol,
                    Rate = rates[i]["quotes"][symbol].GetDecimal("price"),
                    ChangePercentage = rates[i]["quotes"][symbol].GetDecimal("percent_change_24h"),
                    Chart = chart,
                };
                coins.Add(coin);
            }

            return coins;
        }

        public string SearchCommand(string input)
        {
            try
            {
                if (Address.IsValidAddress(input)) //maybe is address
                {
                    return $"address/{input}";
                }

                //token
                var token = Repository.GetToken(input.ToUpperInvariant());
                if (token != null)// token
                {
                    return $"token/{token.Symbol}";
                }

                //app
                var apps = Repository.GetApps();
                var app = apps.SingleOrDefault(a => a.Id == input);
                if (app.Title == input)
                {
                    return $"app/{app.Id}";
                }

                //chain
                var chain = Repository.GetChain(input) ?? Repository.GetChain(input);
                if (chain != null)
                {
                    return $"chain/{chain.ChainInfo.Address}";
                }

                //hash
                var hash = Hash.Parse(input);
                if (hash != null)
                {
                    var tx = Repository.GetTransaction(hash.ToString());
                    if (tx != null)
                    {
                        return $"tx/{tx.Txid}";
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
