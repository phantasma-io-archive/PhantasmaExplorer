using System.Collections.Generic;
using System.Linq;
using Phantasma.Cryptography;
using Phantasma.Explorer.Infrastructure.Interfaces;
using Phantasma.Explorer.Utils;
using Phantasma.Explorer.ViewModels;

namespace Phantasma.Explorer.Controllers
{
    public class AddressesController
    {
        private IRepository Repository { get; set; }
        private decimal SoulRate { get; set; }

        public AddressesController(IRepository repo)
        {
            Repository = repo;
        }

        public List<AddressViewModel> GetAddressList()
        {
            var repoAddressList = Repository.GetAddressList();
            var addressList = new List<AddressViewModel>();
            foreach (var address in repoAddressList)
            {
                var balance = Repository.GetAddressNativeBalance(address);
                var soulToken = Repository.GetTokens().SingleOrDefault(x => x.Symbol == "SOUL");
                var addressVm = AddressViewModel.FromAddress(Repository, address, null);
                addressVm.NativeBalances.Add(new BalanceViewModel { ChainName = "main", Balance = balance, Token = TokenViewModel.FromToken(soulToken, null) }); //this view only shows main chain SOUL balance
                addressVm.Balance = balance;
                addressList.Add(addressVm);
            }
            CalculateAddressSoulValue(addressList);
            return addressList;
        }

        public AddressViewModel GetAddress(string addressText)
        {
            var repoAddress = AddressUtils.ValidateAddress(addressText.Trim());
            if (repoAddress != Address.Null)
            {
                var txs = Repository.GetAddressTransactions(repoAddress);
                var soulToken = Repository.GetTokens().SingleOrDefault(x => x.Symbol == "SOUL");
                SoulRate = CoinUtils.GetCoinRate(CoinUtils.SoulId);
                var tokens = Repository.GetTokens().Where(x => x.Symbol != "SOUL");
                var address = AddressViewModel.FromAddress(Repository, repoAddress, txs);
                var chains = Repository.GetChainNames();

                foreach (var chain in chains)
                {
                    var balance = Repository.GetAddressNativeBalance(repoAddress, chain);
                    var txsCount = Repository.GetAddressTransactionCount(repoAddress, chain);
                    if (balance > 0)
                    {
                        address.NativeBalances.Add(new BalanceViewModel
                        {
                            ChainName = chain,
                            Address = addressText,
                            Balance = balance,
                            TxnCount = txsCount,
                            Token = TokenViewModel.FromToken(soulToken, Explorer.MockLogoUrl, price: SoulRate)
                        });
                    }

                    foreach (var token in tokens)
                    {
                        var tokenBalance = Repository.GetAddressBalance(repoAddress, token, chain);
                        if (tokenBalance > 0)
                        {
                            var existingToken = address.TokenBalance.SingleOrDefault(t => t.Token.Symbol == token.Symbol);
                            // add balance to existing entry
                            if (existingToken != null)
                            {
                                existingToken.Balance += tokenBalance;
                            }
                            //add new
                            address.TokenBalance.Add(new BalanceViewModel
                            {
                                Address = addressText,
                                Balance = tokenBalance,
                                ChainName = chain,
                                Token = TokenViewModel.FromToken(token, Explorer.MockLogoUrl),
                            });
                        }
                    }
                }

                SoulRate = CoinUtils.GetCoinRate(CoinUtils.SoulId); //todo
                CalculateAddressSoulValue(new List<AddressViewModel> { address });
                return address;
            }

            return null;
        }

        private void CalculateAddressSoulValue(List<AddressViewModel> list)//todo
        {
            SoulRate = CoinUtils.GetCoinRate(CoinUtils.SoulId);
            foreach (var address in list)
            {
                var soulBalances = address.NativeBalances.Where(b => b.Token.Symbol == "SOUL");
                foreach (var balanceViewModel in soulBalances)
                {
                    balanceViewModel.Value = balanceViewModel.Balance * SoulRate;
                    address.Value = balanceViewModel.Value;
                }
            }
        }
    }
}