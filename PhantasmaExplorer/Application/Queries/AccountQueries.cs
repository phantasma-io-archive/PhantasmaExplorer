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
        }

        public ICollection<Account> QueryRichList(string tokenSymbol = null, int numberOfAddresses = 20)
        {
            var symbol = string.IsNullOrEmpty(tokenSymbol) ? AppSettings.NativeSymbol : tokenSymbol;

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

        public IQueryable<Transaction> QueryAddressTransactions(string address, string chain = null)
        {
            if (!string.IsNullOrEmpty(chain))
            {
                return _context.Accounts
                    .Include(p => p.AccountTransactions)
                    .ThenInclude(p => p.Transaction)
                    .ThenInclude(p => p.Block)
                    .SingleOrDefault(p => p.Address.Equals(address))?
                    .AccountTransactions
                    .Where(p => p.Transaction.Block.ChainName.Equals(chain) ||
                                p.Transaction.Block.ChainAddress.Equals(chain))
                    .Select(c => c.Transaction)
                    .OrderByDescending(p => p.Timestamp)
                    .AsQueryable();
            }

            return _context.Accounts.Include(p => p.AccountTransactions)
                .ThenInclude(p => p.Transaction)
                .ThenInclude(p => p.Block)
                .SingleOrDefault(p => p.Address.Equals(address))?
                .AccountTransactions
                .Select(c => c.Transaction)
                .OrderByDescending(p => p.Timestamp)
                .AsQueryable();
        }


        public int QueryAddressTransactionCount(string address, string chain = null)
        {
            var account = _context.Accounts
                .Include(p => p.AccountTransactions)
                .ThenInclude(p => p.Transaction)
                .ThenInclude(p => p.Block)
                .SingleOrDefault(p => p.Address.Equals(address));

            if (account == null) return 0;


            if (string.IsNullOrEmpty(chain))
            {
                return account.AccountTransactions.Count;
            }

            if (!string.IsNullOrEmpty(chain))
            {
                return account.AccountTransactions
                    .Count(p => p.Transaction.Block.ChainAddress.Equals(chain)
                                || p.Transaction.Block.ChainName.Equals(chain));
            }

            return 0;
        }

        public bool AddressExists(string address)
        {
            return _context.Accounts.SingleOrDefault(p => p.Address.Equals(address)) != null;
        }
    }
}
