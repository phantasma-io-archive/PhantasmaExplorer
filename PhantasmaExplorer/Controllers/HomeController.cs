using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Phantasma.Core.Types;
using Phantasma.Cryptography;
using Phantasma.Explorer.Application.Queries;
using Phantasma.Explorer.Persistance;
using Phantasma.Explorer.Utils;
using Phantasma.Explorer.ViewModels;

namespace Phantasma.Explorer.Controllers
{
    public class HomeController
    {
        public HomeViewModel GetLastestInfo()
        {
            var context = Explorer.AppServices.GetService<ExplorerDbContext>();

            var blockQuery = new BlockQueries(context);
            var txsQuery = new TransactionQueries(context);
            var chainQuery = new ChainQueries(context);

            var blocks = blockQuery.QueryLastBlocks();
            var transactions = txsQuery.QueryTransactions();
            var blocksVm = blocks.Select(BlockHomeViewModel.FromBlock).ToList();

            var txsVm = transactions.Select(transaction => TransactionHomeViewModel.FromTransaction(transaction, context)).ToList();

            // tx history chart calculation
            var repTxs = txsQuery.QueryTransactions(null, 1000);

            var chart = new Dictionary<string, uint>();

            foreach (var transaction in repTxs)
            {
                var block = transaction.Block;
                DateTime chartTime = new Timestamp(block.Timestamp);
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

            int totalChains = chainQuery.QueryChainCount;
            uint height = chainQuery.QueryChain("main").Height;
            int totalTransactions = txsQuery.QueryTotalChainTransactionCount();

            var vm = new HomeViewModel
            {
                Blocks = blocksVm,
                Transactions = txsVm,
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
                for (int day = 0; day < days; day++)
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
                var context = Explorer.AppServices.GetService<ExplorerDbContext>();

                if (Address.IsValidAddress(input)) //maybe is address
                {
                    return $"address/{input}";
                }

                //token
                var token = new TokenQueries(context).QueryToken(input.ToUpperInvariant());
                if (token != null)// token
                {
                    return $"token/{token.Symbol}";
                }

                //app
                var app = new AppQueries(context).QueryApp(input);
                if (app != null)
                {
                    return $"app/{app.Id}";
                }

                //chain
                var chain = new ChainQueries(context).QueryChain(input);
                if (chain != null)
                {
                    return $"chain/{chain.Address}";
                }

                //hash
                var hash = Hash.Parse(input);
                if (hash != null)
                {
                    var tx = new TransactionQueries(context).QueryTransaction(input);
                    if (tx != null)
                    {
                        return $"tx/{tx.Hash}";
                    }

                    var block = new BlockQueries(context).QueryBlock(input);
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
