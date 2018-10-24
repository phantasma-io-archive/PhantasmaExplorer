using System;
using System.Collections.Generic;
using System.Linq;
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
                var addressVm = AddressViewModel.FromAddress(address);
                addressVm.Balances.Add(new BalanceViewModel("main", balance));
                addressList.Add(addressVm);
            }
            CalculateAddressTokesValue(addressList);
            return addressList;
        }

        public AddressViewModel GetAddress(string addressText)
        {
            var repoAddress = Repository.GetAddress(addressText);
            var address = AddressViewModel.FromAddress(repoAddress);
            var chains = Repository.GetChainNames();
            foreach (var chain in chains)
            {
                var balance = Repository.GetAddressBalance(repoAddress, chain);
                if (balance > 0)
                {
                    address.Balances.Add(new BalanceViewModel(chain, balance));
                }
            }
            SoulRate = CoinUtils.GetCoinRate(2827);
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

        private void CalculateAddressTokesValue(List<AddressViewModel> list)//todo
        {
            foreach (var address in list)
            {
                var mainBalance = address.Balances.FirstOrDefault(p => p.ChainName == "main");
                if (mainBalance != null)
                {
                    address.Value = mainBalance.Balance * SoulRate;
                }
            }
        }
    }
}