using System.Collections.Generic;
using System.Linq;
using Phantasma.Explorer.Domain.Entities;

namespace Phantasma.Explorer.ViewModels
{
    public class AddressViewModel
    {
        public string Address { get; set; }
        public string Name { get; set; }
        public decimal Balance { get; set; }
        public decimal Value { get; set; }

        public List<BalanceViewModel> NativeBalances { get; set; }
        public List<BalanceViewModel> TokenBalance { get; set; }
        public IEnumerable<TransactionViewModel> Transactions { get; set; }

        public static AddressViewModel FromAddress(Account account, List<Token> phantasmaTokens)
        {
            var vm = new AddressViewModel
            {
                Address = account.Address,
                Name = account.Name,
                Value = 0,
                NativeBalances = new List<BalanceViewModel>(),
                TokenBalance = new List<BalanceViewModel>(),
                Transactions = account.AccountTransactions.Select(p => TransactionViewModel.FromTransaction(p.Transaction)).ToList(),
            };

            var soulTokens = account.TokenBalance.Where(p => p.TokenSymbol.Equals("SOUL"));
            var otherTokens = account.TokenBalance.Where(p => !p.TokenSymbol.Equals("SOUL"));

            foreach (var balance in soulTokens)
            {
                vm.NativeBalances.Add(BalanceViewModel.FromAccountBalance(account,
                    balance,
                    phantasmaTokens.SingleOrDefault(p => p.Symbol.Equals(balance.TokenSymbol))));
            }

            vm.Balance = vm.NativeBalances.Sum(p => p.Balance);

            foreach (var balance in otherTokens)
            {
                vm.TokenBalance.Add(BalanceViewModel.FromAccountBalance(account,
                    balance,
                    phantasmaTokens.SingleOrDefault(p => p.Symbol.Equals(balance.TokenSymbol))));
            }

            return vm;
        }
    }
}
