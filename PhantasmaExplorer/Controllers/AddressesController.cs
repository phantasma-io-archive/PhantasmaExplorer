using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Phantasma.Cryptography;
using Phantasma.Explorer.Application;
using Phantasma.Explorer.Application.Queries;
using Phantasma.Explorer.Persistance;
using Phantasma.Explorer.Utils;
using Phantasma.Explorer.ViewModels;

namespace Phantasma.Explorer.Controllers
{
    public class AddressesController : BaseController
    {
        private decimal SoulRate { get; set; }

        public AddressesController() : base(Explorer.AppServices.GetService<ExplorerDbContext>()) { }

        public List<AddressViewModel> GetAddressList()
        {
            var addressQueries = new AccountQueries(_context);
            var tokenQueries = new TokenQueries(_context);

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

        public bool IsAddressStored(string address)
        {
            var addressQueries = new AccountQueries(_context);
            return addressQueries.AddressExists(address);
        }

        public AddressViewModel GetAddress(string addressText, int currentPage, int pageSize = AppSettings.PageSize)
        {
            var addressQueries = new AccountQueries(_context);
            var tokenQueries = new TokenQueries(_context);

            var account = addressQueries.QueryAccount(addressText);

            if (account != null)
            {
                SoulRate = CoinUtils.GetCoinRate(CoinUtils.SoulId);
                var addressVm = AddressViewModel.FromAddress(account, tokenQueries.QueryTokens().ToList());

                addressVm.Transactions = GetAddressTransactions(addressVm.Address, currentPage, pageSize);

                foreach (var addressVmNativeBalance in addressVm.NativeBalances)
                {
                    addressVmNativeBalance.TxnCount = GetTransactionCount(addressVm.Address);
                }

                SoulRate = CoinUtils.GetCoinRate(CoinUtils.SoulId);
                CalculateAddressSoulValue(new List<AddressViewModel> { addressVm });
                return addressVm;
            }

            if (Address.IsValidAddress(addressText))
            {
                return new AddressViewModel { Address = addressText, Balance = 0, Name = "anonymous", Value = 0 };
            }

            return null;
        }

        private List<TransactionViewModel> GetAddressTransactions(string address, int currentPage, int pageSize = AppSettings.PageSize, string chain = null)
        {
            var txQuery = new AccountQueries(_context);

            var query = txQuery.QueryAddressTransactions(address, chain).Skip((currentPage - 1) * pageSize).Take(pageSize);

            return query.AsEnumerable().Select(TransactionViewModel.FromTransaction).ToList();
        }

        public int GetTransactionCount(string address, string chain = null)
        {
            var addressQueries = new AccountQueries(_context);
            return addressQueries.QueryAddressTransactionCount(address, chain);
        }

        private void CalculateAddressSoulValue(List<AddressViewModel> list)
        {
            SoulRate = CoinUtils.GetCoinRate(CoinUtils.SoulId);
            foreach (var address in list)
            {
                var soulBalances = address.NativeBalances.Where(b => b.Token.Symbol == AppSettings.NativeSymbol);
                foreach (var balanceViewModel in soulBalances)
                {
                    balanceViewModel.Value = balanceViewModel.Balance * SoulRate;
                    address.Value = balanceViewModel.Value;
                }
            }
        }
    }
}