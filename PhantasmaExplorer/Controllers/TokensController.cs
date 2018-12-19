using System.Collections.Generic;
using System.Linq;
using Phantasma.Blockchain;
using Phantasma.Blockchain.Tokens;
using Phantasma.Cryptography;
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
            if (token != null && allChains != null)
            {
                foreach (var chain in allChains)
                {
                    if ((token.Flags & TokenFlags.Fungible) != 0)
                    {
                        var balanceSheet = chain.GetTokenBalances(token);
                        balanceSheet.ForEach((address, integer) =>
                        {
                            var vm = new BalanceViewModel
                            {
                                ChainName = chain.Name,
                                Balance = TokenUtils.ToDecimal(integer, token.Decimals),
                                Token = TokenViewModel.FromToken(token, Explorer.MockLogoUrl),
                                Address = address.Text
                            };
                            balances.Add(vm);
                        });
                    }
                    else
                    {
                        var ownershipSheet = chain.GetTokenOwnerships(token);
                        ownershipSheet.ForEach((address, integer) =>
                        {
                            var vm = new BalanceViewModel
                            {
                                ChainName = chain.Name,
                                Balance = integer.Count(),
                                Token = TokenViewModel.FromToken(token, Explorer.MockLogoUrl),
                                Address = address.Text
                            };
                            balances.Add(vm);
                        });
                    }

                }
            }
            return balances;
        }

        public List<TransactionViewModel> GetTransfers(string symbol)
        {
            var temp = new List<TransactionViewModel>();
            var transfers = Repository.GetLastTokenTransfers(symbol, 100).ToList();
            foreach (var tx in transfers)
            {
                var block = Repository.FindBlockForTransaction(tx.Txid);
                temp.Add(TransactionViewModel.FromTransaction(Repository, BlockViewModel.FromBlock(Repository, block), tx));
            }

            return new List<TransactionViewModel>(temp.Where(p => p.AmountTransfer > 0).Take(20));
        }

        public int GetTransactionCount(string symbol)
        {
            return Repository.GetTokenTransfersCount(symbol);
        }

        public List<NftViewModel> GetNftListByAddress(string inputAddress) //todo finish this
        {
            var repoAddress = Repository.GetAddress(inputAddress);
            var nfTokens = Repository.GetTokens().Where(t => !t.Flags.HasFlag(TokenFlags.Fungible));//get only non fungible tokens
            var chains = Repository.GetAllChains();

            var appChain = chains.FirstOrDefault(x => x.Name == "apps");

            var nftList = new List<NftViewModel>();
            if (repoAddress != Address.Null)
            {
                foreach (var chain in chains)
                {
                    foreach (var nfToken in nfTokens)
                    {
                        var ownershipSheet = chain.GetTokenOwnerships(nfToken); //todo move this to repository
                        var ids = ownershipSheet.Get(repoAddress);

                        var viewerURL = appChain.InvokeContract("apps", "GetTokenViewer", nfToken.Symbol).ToString();

                        foreach (var id in ids)
                        {
                            var existingVm = nftList.SingleOrDefault(vm => vm.Symbol == nfToken.Symbol);
                            if (existingVm != null)
                            {
                                existingVm.InfoList.Add(new NftInfoViewModel
                                {
                                    ViewerUrl = viewerURL,
                                    Id = id.ToString(),
                                    Info = "Mock info: " + id + existingVm.Symbol
                                });
                            }
                            else
                            {
                                var newVm = new NftViewModel
                                {
                                    Address = inputAddress,
                                    Symbol = nfToken.Symbol,
                                    InfoList = new List<NftInfoViewModel>
                                    {
                                        new NftInfoViewModel
                                        {
                                            ViewerUrl = viewerURL,
                                            Id = id.ToString(),
                                            Info = "Test info: " + id + nfToken.Symbol
                                        }
                                    }
                                };
                                nftList.Add(newVm);
                            }
                        }
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