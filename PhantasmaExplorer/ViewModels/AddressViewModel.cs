using System.Collections.Generic;
using System.Linq;
using Phantasma.Cryptography;
using Phantasma.Explorer.Infrastructure.Interfaces;
using Transaction = Phantasma.Blockchain.Transaction;

namespace Phantasma.Explorer.ViewModels
{
    public class AddressViewModel
    {
        public string Address { get; set; }
        public string Name { get; set; }
        public List<BalanceViewModel> Balances { get; set; }
        public decimal Balance { get; set; }
        public decimal Value { get; set; }
        public IEnumerable<TransactionViewModel> Transactions { get; set; }

        public static AddressViewModel FromAddress(IRepository repository, Address address)
        {
            var txs = new List<Transaction>();

            return new AddressViewModel
            {
                Address = address.Text,
                Name = repository.NexusChain.LookUpAddress(address),
                Value = 0,
                Balance = 0,
                Balances = new List<BalanceViewModel>(),
                Transactions = txs.Select( tx => TransactionViewModel.FromTransaction(repository, BlockViewModel.FromBlock(repository, tx.Block), tx))
            };
        }
    }
}
