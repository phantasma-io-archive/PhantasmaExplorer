using System;
using System.Collections.Generic;
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
            SoulRate = CoinUtils.GetCoinRate(2827);
            foreach (var address in repoAddressList)
            {
                var balance = Repository.GetAddressBalance(address);
                addressList.Add(AddressViewModel.FromAddress(address, balance));
            }
            CalculateAddressTokesValue(addressList);
            return addressList;
        }

        public AddressViewModel GetAddress(string addressText)
        {
            var repoAddress = Repository.GetAddress(addressText);
            var balance = Repository.GetAddressBalance(repoAddress);
            SoulRate = CoinUtils.GetCoinRate(2827);
            var address = AddressViewModel.FromAddress(repoAddress, balance);
            CalculateAddressTokesValue(new List<AddressViewModel> { address });

            //mock tx
            var mockTransactionList = new List<TransactionViewModel>();
            foreach (var nexusChain in Repository.NexusChain.Chains)
            {
                mockTransactionList.Add(new TransactionViewModel()
                {
                    ChainAddress = nexusChain.Address.Text,
                    Date = DateTime.Now,
                    Hash = "mock",
                    ChainName = nexusChain.Name,
                });
            }

            address.Transactions = mockTransactionList;
            return address;
        }

        private void CalculateAddressTokesValue(List<AddressViewModel> list)
        {
            foreach (var address in list)
            {
                address.Value = address.Balance * SoulRate;
                var das = address.Value.ToString("F3");
            }
        }
    }
}