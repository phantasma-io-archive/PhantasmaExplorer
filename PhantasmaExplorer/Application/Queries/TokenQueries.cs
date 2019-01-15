using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Phantasma.Explorer.Domain.Entities;
using Phantasma.Explorer.Persistance;
using Phantasma.IO;
using Phantasma.Numerics;

namespace Phantasma.Explorer.Application.Queries
{
    public class TokenQueries
    {
        private readonly ExplorerDbContext _context;

        public TokenQueries()
        {
            _context = Explorer.AppServices.GetService<ExplorerDbContext>();
            _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        public ICollection<Token> QueryTokens()
        {
            return _context.Tokens.ToList();
        }

        public ICollection<NonFungibleToken> QueryNfTokens()
        {
            return _context.NonFungibleTokens.ToList();
        }

        public Token QueryToken(string tokenSymbol)
        {
            return _context.Tokens.SingleOrDefault(p => p.Symbol.Equals(tokenSymbol));
        }

        public int QueryTokenTransfersCount(string tokenSymbol)
        {
            int count = 0;
            var eventList = _context.Transactions.OrderByDescending(p => p.Timestamp);
            foreach (var tx in eventList) //todo move this to share
            {
                foreach (var txEvent in tx.Events)
                {
                    var symbol = Serialization.Unserialize<string>(txEvent.Data.Decode()); //todo remove serialization dependency
                    if (symbol.Equals(tokenSymbol))
                    {
                        count++;
                        break;
                    }
                }
            }

            return count;
        }
    }
}