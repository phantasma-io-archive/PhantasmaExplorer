using System.Collections.Generic;
using System.Linq;
using Phantasma.Blockchain;
using Phantasma.Explorer.ViewModels;

namespace Phantasma.Explorer.Controllers
{
    public class TokensController
    {
        public Nexus NexusChain { get; set; } //todo this should be replace with a repository or db instance

        public TokensController(Nexus chain)
        {
            NexusChain = chain;
        }

        public List<TokenViewModel> GetTokens()
        {
            var nexusTokens = NexusChain.Tokens.ToList();
            var tokensList = new List<TokenViewModel>();
            foreach (var token in nexusTokens)
            {
                tokensList.Add(new TokenViewModel
                {
                    Name = token.Name,
                    Symbol = token.Symbol,
                    Decimals = (int) token.GetDecimals(),
                    Description = "Soul is the native asset of Phantasma blockchain",
                    LogoUrl = "https://s2.coinmarketcap.com/static/img/coins/32x32/2827.png",
                    ContractHash = "hash here?",
                    CurrentSupply = (decimal) token.CurrentSupply,
                    MaxSupply = (decimal) token.MaxSupply,
                });
            }

            return tokensList;
        }
    }
}