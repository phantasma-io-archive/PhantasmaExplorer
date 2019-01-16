using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Phantasma.Blockchain.Tokens;
using Phantasma.Explorer.Application.Queries;
using Phantasma.Explorer.Persistance;
using Phantasma.Explorer.Utils;
using Phantasma.Explorer.ViewModels;

namespace Phantasma.Explorer.Controllers
{
    public class TokensController
    {
        private decimal SoulRate { get; set; }

        public TokenViewModel GetToken(string symbol)
        {
            var context = Explorer.AppServices.GetService<ExplorerDbContext>();

            var tokenQuery = new TokenQueries(context);
            var token = tokenQuery.QueryToken(symbol);
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
            var context = Explorer.AppServices.GetService<ExplorerDbContext>();

            var tokenQuery = new TokenQueries(context);
            var tokenList = tokenQuery.QueryTokens();
            var tokensList = new List<TokenViewModel>();

            SoulRate = CoinUtils.GetCoinRate(CoinUtils.SoulId);

            foreach (var token in tokenList)
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
            var context = Explorer.AppServices.GetService<ExplorerDbContext>();

            var accountQuery = new AccountQueries(context);
            var tokenQuery = new TokenQueries(context);

            var token = tokenQuery.QueryToken(symbol);

            List<BalanceViewModel> balances = new List<BalanceViewModel>();

            if (token != null)
            {
                if (token.Fungible)
                {
                    foreach (var account in accountQuery.QueryRichList(symbol, 30))
                    {

                        var accountTokenBalanceList = accountQuery.QueryAccountTokenBalanceList(account.Address, symbol);
                        foreach (var balance in accountTokenBalanceList)
                        {
                            var vm = new BalanceViewModel
                            {
                                ChainName = balance.Chain,
                                Balance = TokenUtils.ToDecimal(balance.Amount, (int)token.Decimals),
                                Token = TokenViewModel.FromToken(token, Explorer.MockLogoUrl),
                                Address = account.Address
                            };
                            balances.Add(vm);
                        }
                    }
                }
                else
                {
                    //todo nft holder list
                }
            }

            return balances;
        }

        public List<TransactionViewModel> GetTransfers(string symbol)
        {
            var context = Explorer.AppServices.GetService<ExplorerDbContext>();

            var txsQuery = new TransactionQueries(context);
            var transfers = txsQuery.QueryLastTokenTransactions(symbol, 100);

            var temp = transfers.Select(TransactionViewModel.FromTransaction).ToList();

            return new List<TransactionViewModel>(temp.Where(p => p.AmountTransfer > 0).Take(20));
        }

        public int GetTransactionCount(string symbol)
        {
            var context = Explorer.AppServices.GetService<ExplorerDbContext>();

            return new TokenQueries(context).QueryTokenTransfersCount(symbol);
        }

        public List<NftViewModel> GetNftListByAddress(string inputAddress) //todo test this
        {
            var context = Explorer.AppServices.GetService<ExplorerDbContext>();

            var accountQuery = new AccountQueries(context);
            var account = accountQuery.QueryAccount(inputAddress);
            var nftList = new List<NftViewModel>();

            if (account != null)
            {
                foreach (var nfToken in account.NonFungibleTokens)
                {
                    var viewerURL = ""; //todo invoke contracts appChain.InvokeContract("apps", "GetTokenViewer", nfToken.Symbol).ToString();

                    var existingVm = nftList.SingleOrDefault(vm => vm.Symbol == nfToken.TokenSymbol);
                    if (existingVm != null)
                    {
                        existingVm.InfoList.Add(new NftInfoViewModel
                        {
                            ViewerUrl = viewerURL,
                            Id = nfToken.Id,
                            Info = "Mock info: " + nfToken.Id + existingVm.Symbol
                        });
                    }
                    else
                    {
                        var newVm = new NftViewModel
                        {
                            Address = inputAddress,
                            Symbol = nfToken.TokenSymbol,
                            InfoList = new List<NftInfoViewModel>
                                    {
                                        new NftInfoViewModel
                                        {
                                            ViewerUrl = viewerURL,
                                            Id = nfToken.Id,
                                            Info = "Test info: " + nfToken.Id + nfToken.TokenSymbol
                                        }
                                    }
                        };
                        nftList.Add(newVm);
                    }
                }
            }

            return nftList;
        }

        //todo
        public string GetViewerUrl(string symbol)
        {
            if (symbol == "NACHO")
            {
                return "https://nacho.men/luchador/body/";
            }

            return string.Empty;
        }
    }
}