using System.Collections.Generic;
using System.Linq;
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
    }
}