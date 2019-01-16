using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
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

        public ICollection<Account> QueryRichList(string tokenSymbol = null, int numberOfAddresses = 20)
        {
            var symbol = string.IsNullOrEmpty(tokenSymbol) ? "SOUL" : tokenSymbol;

            var addressList = new List<Account>();
            var temp = new Dictionary<string, decimal>();
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
                        temp.Add(account.Address, totalTokenBalance);
                    }
                }
            }

            var orderedDictionary = temp.OrderByDescending(p => p.Value).Take(numberOfAddresses).ToDictionary(p => p.Key, q => q.Value);

            foreach (var testKey in orderedDictionary.Keys)
            {
                addressList.Add(_context.Accounts.Single(p => p.Address.Equals(testKey)));
            }

            return addressList;
        }

        public Account QueryAccount(string address)
        {
            return _context.Accounts
                .Include(p => p.AccountTransactions)
                .ThenInclude(p => p.Transaction)
                .ThenInclude(p => p.Block)
                .ThenInclude(p => p.Chain)//todo revisit this, 1so much fk stuff -.-
                .SingleOrDefault(p => p.Address.Equals(address));
        }

        public ICollection<FBalance> QueryAccountTokenBalanceList(string address, string tokenSymbol, string chainName = null)
        {
            IEnumerable<FBalance> fungibleTokens = null;
            var account = _context.Accounts.SingleOrDefault(p => p.Address.Equals(address));

            if (account != null)
            {
                fungibleTokens = string.IsNullOrEmpty(chainName)
                    ? account.TokenBalance.Where(p => p.TokenSymbol.Equals(tokenSymbol))
                    : account.TokenBalance.Where(p => p.TokenSymbol.Equals(tokenSymbol) && p.Chain.Equals(chainName));
            }

            return fungibleTokens?.ToList();
        }

        public ICollection<NonFungibleToken> QueryAccountNftBalance(string address, string tokenSymbol, string chain = null)
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

            return nonFungibleTokens?.ToList();
        }
    }
}
