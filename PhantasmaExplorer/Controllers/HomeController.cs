using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Phantasma.Core.Types;
using Phantasma.Cryptography;
using Phantasma.Explorer.Application;
using Phantasma.Explorer.Application.Queries;
using Phantasma.Explorer.Persistance;
using Phantasma.Explorer.Utils;
using Phantasma.Explorer.ViewModels;

namespace Phantasma.Explorer.Controllers
{
    public class HomeController : BaseController
    {
        public HomeController() : base(Explorer.AppServices.GetService<ExplorerDbContext>()) { }

        public HomeViewModel GetLastestInfo()
        {
            var blockQuery = new BlockQueries(_context);
            var txsQuery = new TransactionQueries(_context);
            var chainQuery = new ChainQueries(_context);

            var blocks = blockQuery.QueryLastBlocks(AppSettings.PageSize);
            var transactions = txsQuery.QueryLastTransactions(15);
            var blocksVm = blocks.Select(BlockHomeViewModel.FromBlock).ToList();

            var txsVm = transactions.Select(TransactionHomeViewModel.FromTransaction).ToList();

            // tx history chart calculation
            var repTxs = txsQuery.QueryLastTransactions(1000, null);

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

        public static List<CoinRateViewModel> GetRateInfo()
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
                if (Address.IsValidAddress(input)) //maybe is address
                {
                    return $"address/{input}";
                }

                //token
                var token = new TokenQueries(_context).QueryToken(input.ToUpperInvariant());
                if (token != null)// token
                {
                    return $"token/{token.Symbol}";
                }

                //app
                var app = new AppQueries(_context).QueryApp(input);
                if (app != null)
                {
                    return $"app/{app.Id}";
                }

                //chain
                var chain = new ChainQueries(_context).QueryChain(input);
                if (chain != null)
                {
                    return $"chain/{chain.Address}";
                }

                //hash
                if (Hash.TryParse(input, out var hash))
                {
                    var tx = new TransactionQueries(_context).QueryTransaction(input);
                    if (tx != null)
                    {
                        return $"tx/{tx.Hash}";
                    }

                    var block = new BlockQueries(_context).QueryBlock(input);
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
