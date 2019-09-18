using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Phantasma.Explorer.Domain.Entities;
using Phantasma.Explorer.Persistance;
using Phantasma.RpcClient.DTOs;

namespace Phantasma.Explorer.Application.Queries
{
    public class TokenQueries
    {
        private readonly ExplorerDbContext _context;

        public TokenQueries(ExplorerDbContext context)
        {
            _context = context;
        }

        public ICollection<Token> QueryTokens()
        {
            return _context.Tokens.ToList();
        }

        public ICollection<NonFungibleToken> QueryAllNonFungibleTokens(string tokenSymbol = null, string chain = null)
        {
            var query = _context.NonFungibleTokens;

            if (string.IsNullOrEmpty(tokenSymbol) && string.IsNullOrEmpty(chain))
            {
                return query.ToList();
            }

            if (!string.IsNullOrEmpty(tokenSymbol) && string.IsNullOrEmpty(chain))
            {
                return _context.NonFungibleTokens.Where(p => p.TokenSymbol.Equals(tokenSymbol)).ToList();
            }

            if (string.IsNullOrEmpty(tokenSymbol) && !string.IsNullOrEmpty(chain))
            {
                return _context.NonFungibleTokens.Where(p => p.Chain.Equals(chain)).ToList();
            }

            if (!string.IsNullOrEmpty(tokenSymbol) && !string.IsNullOrEmpty(chain))
            {
                return _context.NonFungibleTokens.Where(p => p.Chain.Equals(chain) && p.TokenSymbol.Equals(tokenSymbol)).ToList();
            }

            return _context.NonFungibleTokens.Where(p => p.Chain.Equals(chain) && p.TokenSymbol.Equals(tokenSymbol)).ToList();
        }

        public Token QueryToken(string tokenSymbol)
        {
            return _context.Tokens.SingleOrDefault(p => p.Symbol.Equals(tokenSymbol));
        }

        public string QueryNativeTokenName()
        {
            return _context.Tokens.SingleOrDefault(p => (p.Flags & TokenFlags.Fuel) != 0)?.Symbol;
        }

        public ICollection<Transaction> QueryLastTokenTransactions(string tokenSymbol, int amount)
        {
            var txList = new List<Transaction>();

            var token = _context.Tokens
                .Include(p => p.Transactions)
                .ThenInclude(p => p.Block)
                .ThenInclude(p => p.Chain)
                .SingleOrDefault(p => p.Symbol == tokenSymbol);

            return token.Transactions
                .OrderByDescending(p => p.Timestamp)
                .Take(amount)
                .ToList();

            //return transactions.ToList();

            ////.Include(p => p.Block)
            ////.Include(p => p.Block.Chain)
            ////.ToList();

            //foreach (var tx in eventList)
            //{
            //    if (tx.Events != null)
            //    {
            //        foreach (var txEvent in tx.Events)
            //        {
            //            if (txEvent.EventKind == EventKind.TokenSend
            //                || txEvent.EventKind == EventKind.TokenReceive
            //                || txEvent.EventKind == EventKind.TokenEscrow
            //                || txEvent.EventKind == EventKind.TokenMint
            //                || txEvent.EventKind == EventKind.TokenBurn
            //                || txEvent.EventKind == EventKind.TokenStake
            //                || txEvent.EventKind == EventKind.TokenUnstake)
            //            {
            //                var tokenEvent = Serialization.Unserialize<TokenEventData>(txEvent.Data.Decode());
            //                if (!tokenEvent.symbol.Equals(tokenSymbol)) continue;
            //                txList.Add(tx);
            //                break;
            //            }
            //        }
            //    }
            //}

            //return txList.Take(amount).ToList();
        }
    }
}