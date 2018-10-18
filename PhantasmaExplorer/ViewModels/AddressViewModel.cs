using System.Collections.Generic;
using Phantasma.Blockchain;
using Phantasma.Cryptography;

namespace Phantasma.Explorer.ViewModels
{
    public class AddressViewModel
    {
        public string Address { get; set; }
        public string Name { get; set; }
        public decimal Balance { get; set; }
        public decimal Value { get; set; }
        public List<TransactionViewModel> Transactions { get; set; }

        public static AddressViewModel FromAddress(Address address, decimal balance)
        {
            return new AddressViewModel()
            {
                Address = address.Text,
                Name = "Anonymous",
                Balance = balance,
                Value = 0
            };
        }
    }
}
