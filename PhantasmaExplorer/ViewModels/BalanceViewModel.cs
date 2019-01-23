using Phantasma.Blockchain.Tokens;
using Phantasma.Explorer.Application;
using Phantasma.Explorer.Domain.Entities;
using Phantasma.Explorer.Domain.ValueObjects;
using Token = Phantasma.Explorer.Domain.Entities.Token;

namespace Phantasma.Explorer.ViewModels
{
    public class BalanceViewModel
    {
        public string ChainName { get; set; }
        public decimal Balance { get; set; }
        public decimal Value { get; set; }
        public string Address { get; set; }
        public int TxnCount { get; set; }
        public TokenViewModel Token { get; set; } = new TokenViewModel();

        public static BalanceViewModel FromAccountBalance(Account account, FBalance balance, Token token)
        {
            return new BalanceViewModel
            {
                Address = account.Address,
                Token = TokenViewModel.FromToken(token, AppSettings.MockLogoUrl),
                Value = 0,
                Balance = TokenUtils.ToDecimal(balance.Amount, (int)token.Decimals),
                ChainName = balance.Chain,
                TxnCount = account.AccountTransactions.Count//todo
            };
        }
    }
}
