using System.Collections.Generic;
using System.IO;
using Phantasma.Blockchain;
using Phantasma.Explorer.Infrastructure.Interfaces;
using Phantasma.Explorer.Utils;
using Phantasma.Explorer.ViewModels;

namespace Phantasma.Explorer.Controllers
{
    public class TokensController
    {
        public IRepository Repository { get; set; } //todo interface
        private decimal SoulRate { get; set; }


        public TokensController(IRepository repo)
        {
            Repository = repo;
            SoulRate = CoinUtils.GetCoinRate(2827);//todo update
        }

        public TokenViewModel GetToken(string symbol)
        {
            var token = Repository.GetToken(symbol);
            if (token != null)
            {
                return TokenViewModel.FromToken(token, //todo
                    "Soul is the native asset of Phantasma blockchain",
                    "https://s2.coinmarketcap.com/static/img/coins/32x32/2827.png",
                    SoulRate);
            }

            return null;
        }

        public List<TokenViewModel> GetTokens()
        {
            var nexusTokens = Repository.GetTokens();
            var tokensList = new List<TokenViewModel>();
            foreach (var token in nexusTokens)
            {
                tokensList.Add(TokenViewModel.FromToken(token, //todo
                    "Soul is the native asset of Phantasma blockchain",
                    "https://s2.coinmarketcap.com/static/img/coins/32x32/2827.png",
                    SoulRate));
            }

            return tokensList;
        }

        public List<BalanceViewModel> GetHolders(string symbol) //todo
        {
            var mainChain = Repository.GetChainByName("main");
            var token = Repository.GetToken(symbol);
            List<BalanceViewModel> balances = new List<BalanceViewModel>();
            if (token != null && mainChain != null)
            {
                var balanceSheet = mainChain.GetTokenBalances(token);
                balanceSheet.ForEach((address, integer) =>
                {
                    var vm = new BalanceViewModel(mainChain.Name, TokenUtils.ToDecimal(integer))
                    {
                        TokenSymbol = token.Symbol,
                        Address = address.Text
                    };
                    balances.Add(vm);
                });
            }
            return balances;
        }
    }
}