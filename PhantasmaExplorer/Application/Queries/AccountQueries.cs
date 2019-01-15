using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Phantasma.Blockchain.Tokens;
using Phantasma.Explorer.Domain.Entities;
using Phantasma.Explorer.Domain.ValueObjects;
using Phantasma.Explorer.Persistance;

namespace Phantasma.Explorer.Application.Queries
{
    public class AccountQueries
    {
        private readonly ExplorerDbContext _context;

        public AccountQueries(ExplorerDbContext context)
        {
            _context = context;
            _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        public List<Account> QueryRichList(string tokenSymbol = null, int numberOfAddresses = 20)
        {
            var symbol = string.IsNullOrEmpty(tokenSymbol) ? "SOUL" : tokenSymbol;

            var addressList = new List<Account>();

            foreach (var account in _context.Accounts)
            {
                decimal totalTokenBalance = 0;
                var tokenbalance = account.TokenBalance.Where(p => p.TokenSymbol == symbol).ToList();

                if (tokenbalance.Any())
                {
                    foreach (var balance in tokenbalance)
                    {
                        totalTokenBalance += decimal.Parse(balance.Amount);
                    }

                    if (totalTokenBalance > 0)
                    {
                        addressList.Add(account);
                    }
                }
            }

            return addressList.OrderByDescending(p => p.TokenBalance.OrderByDescending(c => decimal.Parse(c.Amount))).ToList();
        }

        public Account QueryAccount(string address)
        {
            return _context.Accounts.SingleOrDefault(p => p.Address.Equals(address));
        }

        public string QueryAccountName(string address)
        {
            return _context.Accounts.SingleOrDefault(p => p.Address.Equals(address))?.Name;
        }

        public IEnumerable<FBalance> QueryAccountTokenBalanceList(string address, string tokenSymbol, string chainName = null)
        {
            IEnumerable<FBalance> fungibleTokens = null;
            int tokenDecimals = (int)_context.Tokens.Single(p => p.Symbol.Equals(tokenSymbol)).Decimals;
            var account = _context.Accounts.SingleOrDefault(p => p.Address.Equals(address));

            if (account != null)
            {
                fungibleTokens = string.IsNullOrEmpty(chainName)
                    ? account.TokenBalance.Where(p => p.TokenSymbol.Equals(tokenSymbol))
                    : account.TokenBalance.Where(p => p.TokenSymbol.Equals(tokenSymbol) && p.Chain.Equals(chainName));
            }

            return fungibleTokens;
        }

        public decimal QueryAccountTokenBalance(string address, string tokenSymbol, string chainName = null)
        {
            decimal amount = 0;
            int tokenDecimals = (int)_context.Tokens.Single(p => p.Symbol.Equals(tokenSymbol)).Decimals;
            var account = _context.Accounts.SingleOrDefault(p => p.Address.Equals(address));

            if (account != null)
            {
                if (string.IsNullOrEmpty(chainName))
                {
                    amount += account.TokenBalance.Where(fBalance => fBalance.TokenSymbol.Equals(tokenSymbol)).Sum(fBalance => TokenUtils.ToDecimal(fBalance.Amount, tokenDecimals));
                }
                else
                {
                    amount = TokenUtils.ToDecimal(
                        account.TokenBalance
                            .SingleOrDefault(p => p.TokenSymbol.Equals(tokenSymbol) && p.Chain.Equals(chainName))
                            ?.Amount, tokenDecimals);
                }
            }

            return amount;
        }

        public IEnumerable<NonFungibleToken> QueryAccountNFTBalance(string address, string tokenSymbol, string chain = null)
        {
            IEnumerable<NonFungibleToken> nonFungibleTokens = null;
            var account = _context.Accounts.SingleOrDefault(p => p.Address.Equals(address));

            if (account != null)
            {
                if (string.IsNullOrEmpty(chain))
                {
                    nonFungibleTokens = account.NonFungibleTokens
                        .Where(p => p.TokenSymbol.Equals(tokenSymbol));
                }
                else
                {
                    nonFungibleTokens = account.NonFungibleTokens
                        .Where(p => p.Chain.Equals(chain) && p.TokenSymbol.Equals(tokenSymbol));
                }
            }

            return nonFungibleTokens;
        }
    }
}
