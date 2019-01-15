using System.Collections.Generic;
using System.Linq;
using Phantasma.Explorer.Application.Queries;
using Phantasma.Explorer.Utils;
using Phantasma.Explorer.ViewModels;

namespace Phantasma.Explorer.Controllers
{
    public class AddressesController
    {
        private decimal SoulRate { get; set; }

        public List<AddressViewModel> GetAddressList()
        {
            var addressQueries = new AccountQueries();
            var tokenQueries = new TokenQueries();
            var addressList = new List<AddressViewModel>();

            var list = addressQueries.QueryRichList(numberOfAddresses: 30);

            foreach (var account in list)
            {
                var addressVm = AddressViewModel.FromAddress(account, tokenQueries.QueryTokens().ToList());
                CalculateAddressSoulValue(new List<AddressViewModel> { addressVm });
                addressList.Add(addressVm);
            }

            CalculateAddressSoulValue(addressList);

            return addressList;
        }

        public AddressViewModel GetAddress(string addressText)
        {
            var addressQueries = new AccountQueries();
            var tokenQueries = new TokenQueries();
            var transactionQueries = new TransactionQueries();
            var account = addressQueries.QueryAccount(addressText);

            if (account != null)
            {
                SoulRate = CoinUtils.GetCoinRate(CoinUtils.SoulId);
                var addressVm = AddressViewModel.FromAddress(account, tokenQueries.QueryTokens().ToList());

                foreach (var addressVmNativeBalance in addressVm.NativeBalances)
                {
                    addressVmNativeBalance.TxnCount =
                        transactionQueries.QueryAddressTransactionCount(addressVm.Address,
                            addressVmNativeBalance.ChainName);
                }

                SoulRate = CoinUtils.GetCoinRate(CoinUtils.SoulId);
                CalculateAddressSoulValue(new List<AddressViewModel> { addressVm });
                return addressVm;
            }

            return null;
        }

        private void CalculateAddressSoulValue(List<AddressViewModel> list) //todo
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