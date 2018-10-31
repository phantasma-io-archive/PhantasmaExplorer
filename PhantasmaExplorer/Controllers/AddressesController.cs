using System.Collections.Generic;
using System.Linq;
using Phantasma.Blockchain.Tokens;
using Phantasma.Cryptography;
using Phantasma.Explorer.Infrastructure.Interfaces;
using Phantasma.Explorer.Utils;
using Phantasma.Explorer.ViewModels;

namespace Phantasma.Explorer.Controllers
{
    public class AddressesController
    {
        public IRepository Repository { get; set; }
        private decimal SoulRate { get; set; }

        public AddressesController(IRepository repo)
        {
            Repository = repo;
        }

        public List<AddressViewModel> GetAddressList()
        {
            var repoAddressList = Repository.GetAddressList();
            var addressList = new List<AddressViewModel>();
            SoulRate = CoinUtils.GetCoinRate(CoinUtils.SoulId);
            foreach (var address in repoAddressList)
            {
                var balance = Repository.GetAddressNativeBalance(address);
                var addressVm = AddressViewModel.FromAddress(Repository, address, null);
                addressVm.Balances.Add(new BalanceViewModel { ChainName = "main", Balance = balance });
                addressList.Add(addressVm);
            }
            CalculateAddressTokenValue(addressList);
            return addressList;
        }

        public AddressViewModel GetAddress(string addressText)
        {
            var repoAddress = Repository.GetAddress(addressText);
            if (repoAddress != Address.Null)
            {
                var txs = Repository.GetAddressTransactions(repoAddress);
                var tokens = Repository.GetTokens().Where(x => x.Symbol != "SOUL");//todo check
                var address = AddressViewModel.FromAddress(Repository, repoAddress, txs);
                var chains = Repository.GetChainNames();
                foreach (var chain in chains)
                {
                    var balance = Repository.GetAddressNativeBalance(repoAddress, chain);
                    var txsCount = Repository.GetAddressTransactionCount(repoAddress, chain);
                    if (balance > 0)
                    {
                        address.Balances.Add(new BalanceViewModel
                        {
                            ChainName = chain,
                            Address = addressText,
                            Balance = balance,
                            TxnCount = txsCount,
                        });
                    }

                    foreach (var token in tokens)
                    {
                        var tokenBalance = Repository.GetAddressBalance(repoAddress, token, chain);
                        if (tokenBalance > 0)
                        {
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
                CalculateAddressTokenValue(new List<AddressViewModel> { address });
                return address;
            }

            return null;
        }

        private void CalculateAddressTokenValue(List<AddressViewModel> list)//todo
        {
            foreach (var address in list)
            {
                var mainBalance = address.Balances.FirstOrDefault(p => p.ChainName == "main");
                if (mainBalance != null)
                {
                    address.Balance = mainBalance.Balance;
                    address.Value = mainBalance.Balance * SoulRate;
                }
            }
        }
    }
}