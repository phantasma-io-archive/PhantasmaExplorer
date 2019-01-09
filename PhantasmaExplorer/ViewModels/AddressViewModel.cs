using System.Collections.Generic;
using System.Linq;
using Phantasma.Cryptography;
using Phantasma.Explorer.Infrastructure.Interfaces;
using Phantasma.RpcClient.DTOs;

namespace Phantasma.Explorer.ViewModels
{
    public class AddressViewModel
    {
        public string Address { get; set; }
        public string Name { get; set; }
        public List<BalanceViewModel> NativeBalances { get; set; }
        public List<BalanceViewModel> TokenBalance { get; set; }
        public decimal Balance { get; set; }
        public decimal Value { get; set; }
        public IEnumerable<TransactionViewModel> Transactions { get; set; }

        public static AddressViewModel FromAddress(IRepository repository, Address address, IEnumerable<TransactionDto> txs)
        {
            return new AddressViewModel
            {
                Address = address.Text,
                Name = repository.GetAddressName(address.Text),
                Value = 0,
                Balance = 0,
                NativeBalances = new List<BalanceViewModel>(),
                TokenBalance = new List<BalanceViewModel>(),
                Transactions = txs?.Select(tx => TransactionViewModel.FromTransaction(repository, BlockViewModel.FromBlock(repository, repository.FindBlockForTransaction(tx.Txid)), tx))
            };
        }
    }
}
