using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Phantasma.Explorer.Application;
using Phantasma.Explorer.Application.Queries;
using Phantasma.Explorer.Persistance;
using Phantasma.Explorer.Utils;
using Phantasma.Explorer.ViewModels;
using Phantasma.Numerics;
using Phantasma.RpcClient.DTOs;

namespace Phantasma.Explorer.Controllers
{
    public class TokensController : BaseController
    {
        private decimal SoulRate { get; set; }

        public TokensController() : base(Explorer.AppServices.GetService<ExplorerDbContext>()) { }

        public TokenViewModel GetToken(string symbol)
        {
            var tokenQuery = new TokenQueries(_context);
            var token = tokenQuery.QueryToken(symbol);

            if (token != null)
            {
                SoulRate = token.Symbol == "SOUL" ? CoinUtils.GetCoinRate(CoinUtils.SoulId) : 0;

                return TokenViewModel.FromToken(token,
                    AppSettings.MockLogoUrl,
                    SoulRate);
            }

            return null;
        }

        public List<TokenViewModel> GetTokens()
        {
            var tokenQuery = new TokenQueries(_context);
            var tokenList = tokenQuery.QueryTokens();
            var tokensList = new List<TokenViewModel>();

            SoulRate = CoinUtils.GetCoinRate(CoinUtils.SoulId);

            foreach (var token in tokenList)
            {
                SoulRate = token.Symbol == "SOUL" ? CoinUtils.GetCoinRate(CoinUtils.SoulId) : 0;
                tokensList.Add(TokenViewModel.FromToken(token,
                    AppSettings.MockLogoUrl,
                    SoulRate));
            }

            return tokensList;
        }

        public List<BalanceViewModel> GetHolders(string symbol)
        {
            var accountQuery = new AccountQueries(_context);
            var tokenQuery = new TokenQueries(_context);

            var token = tokenQuery.QueryToken(symbol);

            var balances = new List<BalanceViewModel>();

            if (token == null) return balances;

            if ((token.Flags & TokenFlags.Fungible) != 0)
            {
                foreach (var account in accountQuery.QueryRichList(30, symbol))
                {
                    var accountTokenBalanceList = accountQuery.QueryAccountTokenBalanceList(account.Address, symbol);

                    foreach (var balance in accountTokenBalanceList)
                    {
                        var existingEntry = balances.SingleOrDefault(p => p.Address.Equals(account.Address));
                        if (existingEntry != null)
                        {
                            existingEntry.Balance += UnitConversion.ToDecimal(balance.Amount, (int)token.Decimals);
                        }
                        else
                        {
                            var vm = new BalanceViewModel
                            {
                                ChainName = balance.Chain,
                                Balance = UnitConversion.ToDecimal(balance.Amount, (int)token.Decimals),
                                Token = TokenViewModel.FromToken(token, AppSettings.MockLogoUrl),
                                Address = account.Address
                            };
                            balances.Add(vm);
                        }
                    }
                }
            }
            else
            {
                var nftList = tokenQuery.QueryAllNonFungibleTokens(symbol);
                foreach (var nonFungibleToken in nftList)
                {
                    var vm = new BalanceViewModel
                    {
                        ChainName = nonFungibleToken.Chain,
                        Token = TokenViewModel.FromToken(token, AppSettings.MockLogoUrl),
                        Address = nonFungibleToken.AccountAddress
                    };
                    vm.Balance = nftList.Count(p => p.AccountAddress.Equals(vm.Address));
                    balances.Add(vm);
                }
            }

            return balances;
        }

        public List<TransactionViewModel> GetTransfers(string symbol, int amount = AppSettings.PageSize)
        {
            var txsQuery = new TransactionQueries(_context);
            var transfers = txsQuery.QueryLastTokenTransactions(symbol, amount);

            var temp = transfers.Select(TransactionViewModel.FromTransaction).ToList();

            return new List<TransactionViewModel>(temp.Where(p => p.AmountTransfer > 0).Take(amount));
        }

        public List<NftViewModel> GetNftListByAddress(string inputAddress) //todo redo this after rpc stuff
        {
            var accountQuery = new AccountQueries(_context);
            var account = accountQuery.QueryAccount(inputAddress);
            var nftList = new List<NftViewModel>();

            if (account != null)
            {
                foreach (var nfToken in account.NonFungibleTokens)
                {
                    var viewerURL = "https://nacho.men/luchador/$ID"; //todo invoke contracts appChain.InvokeContract("apps", "GetTokenViewer", nfToken.Symbol).ToString();

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
    }
}