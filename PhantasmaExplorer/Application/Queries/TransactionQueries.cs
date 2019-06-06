using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Phantasma.Blockchain.Contracts.Native;
using Phantasma.Explorer.Domain.Entities;
using Phantasma.Explorer.Persistance;
using Phantasma.Numerics;
using Phantasma.RpcClient.DTOs;
using Phantasma.Storage;

namespace Phantasma.Explorer.Application.Queries
{
    public class TransactionQueries
    {
        private readonly ExplorerDbContext _context;

        public TransactionQueries(ExplorerDbContext context)
        {
            _context = context;
        }

        public IQueryable<Transaction> QueryTransactions(string chain)
        {
            if (!string.IsNullOrEmpty(chain))
            {
                return _context.Transactions
                    .Where(p => p.Block.ChainAddress.Equals(chain) || p.Block.ChainName.Equals(chain))
                    .OrderByDescending(p => p.Timestamp)
                    .Include(p => p.Block);
            }

            return _context.Transactions
                .OrderByDescending(p => p.Timestamp)
                .Include(p => p.Block);
        }

        public ICollection<Transaction> QueryLastTransactions(int amount, string chain = null)
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

        public Transaction QueryTransaction(string hash)
        {
            return _context.Transactions
                .Include(p => p.Block)
                .Include(p => p.Block.Chain)
                .SingleOrDefault(p => p.Hash.Equals(hash));
        }

        public int QueryTotalChainTransactionCount(string chain = null)
        {
            if (string.IsNullOrEmpty(chain)) //total of all chains
            {
                return _context.Transactions.Count();
            }

            return _context.Transactions
                .Count(p => p.Block.ChainAddress.Equals(chain) || p.Block.ChainName.Equals(chain));
        }

        public ICollection<Transaction> QueryLastTokenTransactions(string tokenSymbol, int amount)
        {
            var txList = new List<Transaction>();

            var eventList = _context.Transactions
                .OrderByDescending(p => p.Timestamp)
                .Include(p => p.Block)
                .Include(p => p.Block.Chain)
                .ToList();

            foreach (var tx in eventList)
            {
                if (tx.Events != null)
                {
                    foreach (var txEvent in tx.Events)
                    {
                        if (txEvent.EventKind == EventKind.TokenSend
                            || txEvent.EventKind == EventKind.TokenReceive
                            || txEvent.EventKind == EventKind.TokenEscrow
                            || txEvent.EventKind == EventKind.TokenMint
                            || txEvent.EventKind == EventKind.TokenBurn
                            || txEvent.EventKind == EventKind.TokenStake
                            || txEvent.EventKind == EventKind.TokenUnstake)
                        {
                            var tokenEvent = Serialization.Unserialize<TokenEventData>(txEvent.Data.Decode());
                            if (!tokenEvent.symbol.Equals(tokenSymbol)) continue;
                            txList.Add(tx);
                            break;
                        }
                    }
                }
            }

            return txList.Take(amount).ToList();
        }
    }
}
