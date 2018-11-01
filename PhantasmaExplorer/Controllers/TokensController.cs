using System.Collections.Generic;
using System.Linq;
using Phantasma.Blockchain;
using Phantasma.Blockchain.Tokens;
using Phantasma.Explorer.Infrastructure.Interfaces;
using Phantasma.Explorer.Utils;
using Phantasma.Explorer.ViewModels;

namespace Phantasma.Explorer.Controllers
{
    public class TokensController
    {
        private IRepository Repository { get; }
        private decimal SoulRate { get; set; }

        public TokensController(IRepository repo)
        {
            Repository = repo;
        }

        public TokenViewModel GetToken(string symbol)
        {
            var token = Repository.GetToken(symbol);
            var tranfers = GetTransactionCount(symbol);
            if (token != null)
            {
                SoulRate = token.Symbol == "SOUL" ? CoinUtils.GetCoinRate(CoinUtils.SoulId) : 0;

                return TokenViewModel.FromToken(token,
                    Explorer.MockLogoUrl,
                    tranfers,
                    SoulRate);
            }

            return null;
        }

        public List<TokenViewModel> GetTokens()
        {
            var nexusTokens = Repository.GetTokens();
            var tokensList = new List<TokenViewModel>();
            SoulRate = CoinUtils.GetCoinRate(CoinUtils.SoulId);
            foreach (var token in nexusTokens)
            {
                var tranfers = GetTransactionCount(token.Symbol);
                SoulRate = token.Symbol == "SOUL" ? CoinUtils.GetCoinRate(CoinUtils.SoulId) : 0;
                tokensList.Add(TokenViewModel.FromToken(token,
                    Explorer.MockLogoUrl,
                    tranfers,
                    SoulRate));
            }

            return tokensList;
        }

        public List<BalanceViewModel> GetHolders(string symbol) //todo
        {
            var allChains = Repository.GetAllChains();
            var token = Repository.GetToken(symbol);
            List<BalanceViewModel> balances = new List<BalanceViewModel>();
            if (token != null && allChains != null && (token.Flags & TokenFlags.Fungible) != 0)
            {
                foreach (var chain in allChains)
                {
                    var balanceSheet = chain.GetTokenBalances(token);
                    balanceSheet.ForEach((address, integer) =>
                    {
                        var vm = new BalanceViewModel
                        {
                            ChainName = chain.Name,
                            Balance = TokenUtils.ToDecimal(integer, token.Decimals),
                            Token = TokenViewModel.FromToken(token, Explorer.MockLogoUrl, 0, 0),
                            Address = address.Text
                        };
                        balances.Add(vm);
                    });
                }
            }
            return balances;
        }

        public List<TransactionViewModel> GetTransfers(string symbol)
        {
            var temp = new List<TransactionViewModel>();
            var transfers = Repository.GetLastTokenTransfers(symbol, 100).ToList();
            foreach (var transfer in transfers)
            {
                temp.Add(TransactionViewModel.FromTransaction(Repository, BlockViewModel.FromBlock(Repository, transfer.Block), transfer));
            }

            return new List<TransactionViewModel>(temp.Where(p => p.AmountTransfer > 0).Take(20));
        }

        public int GetTransactionCount(string symbol)
        {
            return Repository.GetTokenTransfersCount(symbol);
        }
    }
}