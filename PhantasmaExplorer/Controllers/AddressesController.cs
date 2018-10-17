using System;
using System.Collections.Generic;
using Phantasma.Blockchain;
using Phantasma.Cryptography;
using Phantasma.Explorer.Utils;
using Phantasma.Explorer.ViewModels;

namespace Phantasma.Explorer.Controllers
{
    public class AddressesController
    {
        public Nexus NexusChain { get; set; } //todo this should be replace with a repository or db instance

        public static decimal SoulRate { get; private set; }


        public AddressesController(Nexus chain)
        {
            NexusChain = chain;
        }

        public List<AddressViewModel> GetAddressList()
        {
            var addressList = new List<AddressViewModel>();

            var targetAddress = Address.FromText("PGasVpbFYdu7qERihCsR22nTDQp1JwVAjfuJ38T8NtrCB"); //todo remove hack
            var ownerKey = KeyPair.FromWIF("L2G1vuxtVRPvC6uZ1ZL8i7Dbqxk9VPXZMGvZu9C3LXpxKK51x41N");

            SoulRate = CoinUtils.GetCoinRate(2827);
            addressList.Add(AddressViewModel.FromAddress(NexusChain, ownerKey.Address, null, SoulRate));
            addressList.Add(AddressViewModel.FromAddress(NexusChain, targetAddress, null, SoulRate));

            return addressList;
        }

        public AddressViewModel GetAddress(string addressText)
        {
            var address = Address.FromText(addressText);
            SoulRate = CoinUtils.GetCoinRate(2827);
            var mockTransactionList = new List<TransactionViewModel>();
            foreach (var nexusChain in NexusChain.Chains)
            {
                mockTransactionList.Add(new TransactionViewModel()
                {
                    ChainAddress = nexusChain.Address.Text,
                    Date = DateTime.Now,
                    Hash = "mock",
                    ChainName = nexusChain.Name,
                });
            }

            return AddressViewModel.FromAddress(NexusChain, address, mockTransactionList, SoulRate);
        }
    }
}