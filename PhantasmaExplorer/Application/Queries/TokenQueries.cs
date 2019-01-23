using System.Collections.Generic;
using System.Linq;
using Phantasma.Explorer.Domain.Entities;
using Phantasma.Explorer.Persistance;

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

        public ICollection<NonFungibleToken> QueryAllNonFungibleTokens(string tokenSymbol, string chain = null)
        {
            if (string.IsNullOrEmpty(chain))
            {
                return _context.NonFungibleTokens.ToList();
            }

            return _context.NonFungibleTokens.Where(p => p.Chain.Equals(chain)).ToList();
        }

        public Token QueryToken(string tokenSymbol)
        {
            return _context.Tokens.SingleOrDefault(p => p.Symbol.Equals(tokenSymbol));
        }
    }
}