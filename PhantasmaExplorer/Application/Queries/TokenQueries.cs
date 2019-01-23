using System.Collections.Generic;
using System.Linq;
using Phantasma.Blockchain.Contracts.Native;
using Phantasma.Explorer.Domain.Entities;
using Phantasma.Explorer.Persistance;
using Phantasma.IO;
using Phantasma.Numerics;

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

        public int QueryTokenTransfersCount(string tokenSymbol)
        {
            var eventList = _context.Transactions;
            //foreach (var tx in eventList) //todo test speed against linq
            //{
            //    var tokenSendEvent = tx.Events.SingleOrDefault(p => p.EventKind.Equals(EventKind.TokenSend));
            //    if (tokenSendEvent == null) continue;
            //    TokenEventData eventData = Serialization.Unserialize<TokenEventData>(tokenSendEvent.Data.Decode()); //todo remove serialization dependency
            //    if (!eventData.symbol.Equals(tokenSymbol)) continue;
            //    count++;
            //}

            return (from tx in eventList
                    select tx.Events.SingleOrDefault(p => p.EventKind.Equals(EventKind.TokenSend))
                into tokenSendEvent
                    where tokenSendEvent != null
                    select Serialization.Unserialize<TokenEventData>(tokenSendEvent.Data.Decode()))
                    .Count(eventData => eventData.symbol.Equals(tokenSymbol));
        }
    }
}