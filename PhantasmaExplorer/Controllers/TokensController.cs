using System.Collections.Generic;
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

        public void GetHolders(string symbol) //todo
        {
            var nexusChain = Repository.GetChain("main");
        }
    }
}