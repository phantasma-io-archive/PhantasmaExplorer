using System.Collections.Generic;
using Phantasma.Cryptography;

namespace Phantasma.Explorer.ViewModels
{
    public class AddressViewModel
    {
        public string Address { get; set; }
        public string Name { get; set; }
        public List<BalanceViewModel> Balances { get; set; }
        public decimal Value { get; set; }
        public List<TransactionViewModel> Transactions { get; set; }

        public static AddressViewModel FromAddress(Address address)
        {
            return new AddressViewModel
            {
                Address = address.Text,
                Name = "Anonymous",
                Value = 0,
                Balances = new List<BalanceViewModel>()
            };
        }
    }
}
