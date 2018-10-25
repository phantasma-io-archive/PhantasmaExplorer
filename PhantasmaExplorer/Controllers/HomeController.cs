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
                txs.Add(TransactionViewModel.FromTransaction(Repository.NexusChain, BlockViewModel.FromBlock(tempBlock), transaction, null));
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
                    chart[chartKey] += 200;
                }
                else
                {
                    chart[chartKey] = 1;
                }
            }

            int totalChains = Repository.GetAllChains().Count; //todo repo
            uint height = Repository.GetChainByName("main").BlockHeight; //todo repo
            int totalTransactions = 0; //todo

            var info = CoinUtils.GetCoinInfo(2827, "BTC");
            var marketCap = info["quotes"]["USD"].GetDecimal("market_cap");

            //USD
            var soulUsd = info["quotes"]["USD"].GetDecimal("price");
            var soulUsdChange = info["quotes"]["USD"].GetDecimal("percent_change_24h");
            CoinRateViewModel soulUsdVm = new CoinRateViewModel
            {
                Coin = "SOUL/USD",
                ChangePercentage = soulUsdChange,
                Rate = soulUsd
            };

            //USD
            var soulBtc = info["quotes"]["USD"].GetDecimal("price");
            var soulBtcChange = info["quotes"]["USD"].GetDecimal("percent_change_24h");
            CoinRateViewModel soulBtcVm = new CoinRateViewModel
            {
                Coin = "SOUL/BTC",
                ChangePercentage = soulBtcChange,
                Rate = soulBtc
            };

            //ETH
            info = CoinUtils.GetCoinInfo(2827, "ETH");
            var soulEth = info["quotes"]["ETH"].GetDecimal("price");
            var soulEthChange = info["quotes"]["ETH"].GetDecimal("percent_change_24h");
            CoinRateViewModel soulEthdVm = new CoinRateViewModel
            {
                Coin = "SOUL/ETH",
                ChangePercentage = soulEthChange,
                Rate = soulEth
            };

            var vm = new HomeViewModel
            {
                Blocks = blocks.OrderByDescending(b => b.Timestamp).ToList(),
                Transactions = txs,
                Chart = chart,
                TotalTransactions = totalTransactions,
                TotalChains = totalChains,
                BlockHeight = height,
                MarketCap = marketCap,
                SOULBTC = soulBtcVm,
                SOULETH = soulEthdVm,
                SOULUSD = soulUsdVm
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
