using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Phantasma.Explorer.Domain.Entities;
using Phantasma.Explorer.Persistance;
using Phantasma.IO;
using Phantasma.Numerics;

namespace Phantasma.Explorer.Application.Queries
{
    public class TransactionQueries
    {
        private readonly ExplorerDbContext _context;

        public TransactionQueries(ExplorerDbContext context)
        {
            _context = context;
            _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        public ICollection<Transaction> QueryTransactions(string chain = null, int amount = 20)
        {
            if (string.IsNullOrEmpty(chain)) //no specific chain
            {
                return _context.Transactions
                    .OrderByDescending(p => p.Timestamp)
                    .Include(p => p.Block)
                    .Take(amount)
                    .ToList();
            }

            return _context.Transactions
                .Include(p => p.Block)
                .Where(p => p.Block.Chain.Address.Equals(chain) || p.Block.Chain.Name.Equals(chain))
                .OrderByDescending(p => p.Timestamp)
                .Take(amount)
                .ToList();
        }

        public ICollection<Transaction> QueryAddressTransactions(string address, int amount = 20)
        {
            return _context.Accounts.SingleOrDefault(p => p.Address.Equals(address))?
                .AccountTransactions
                .Select(c => c.Transaction)
                .ToList();
        }

        public Transaction QueryTransaction(string hash)
        {
            return _context.Transactions
                .Include(p => p.Block)
                .Include(p => p.Block.Chain)
                .SingleOrDefault(p => p.Hash.Equals(hash));
        }

        public int QueryAddressTransactionCount(string address, string chain = null)
        {
            var account = _context.Accounts
                .Include(p => p.AccountTransactions)
                .ThenInclude(p => p.Transaction)
                .ThenInclude(p => p.Block)
                .ThenInclude(p => p.Chain)
                .SingleOrDefault(p => p.Address.Equals(address));

            if (account == null) return 0;

            if (string.IsNullOrEmpty(chain))
            {
                return account.AccountTransactions.Count;
            }

            return account.AccountTransactions
                .Count(p => p.Transaction.Block.Chain.Address.Equals(chain)
                            || p.Transaction.Block.Chain.Name.Equals(chain));
        }

        public int QueryTotalChainTransactionCount(string chain = null)
        {
            if (string.IsNullOrEmpty(chain)) //total of all chains
            {
                return _context.Transactions.Count();
            }

            var contextChain = _context.Chains.SingleOrDefault(p => p.Address.Equals(chain) || p.Name.Equals(chain));

            if (contextChain == null) return 0;

            return contextChain.Blocks.Select(p => p.Transactions.Count).Sum();
        }

        public ICollection<Transaction> QueryLastTokenTransactions(string tokenSymbol, int amount = 20)
        {
            var txList = new List<Transaction>();

            var eventList = _context.Transactions
                .OrderByDescending(p => p.Timestamp)
                .Include(p => p.Block)
                .Include(p => p.Block.Chain)
                .ToList();

            foreach (var tx in eventList) //todo move this to share
            {
                foreach (var txEvent in tx.Events)
                {
                    var symbol = Serialization.Unserialize<string>(txEvent.Data.Decode()); //todo remove serialization dependency
                    if (symbol.Equals(tokenSymbol))
                    {
                        txList.Add(tx);
                        break;
                    }
                }
            }

            return txList.Take(amount).ToList();
        }
    }
}
