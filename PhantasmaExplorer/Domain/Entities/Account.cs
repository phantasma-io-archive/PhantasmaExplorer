using System.Collections.Generic;
using Phantasma.Explorer.Domain.ValueObjects;

namespace Phantasma.Explorer.Domain.Entities
{
    public class Account
    {
        public Account()
        {
            TokenBalance = new HashSet<FungibleBalance>();
            NonFungibleTokens = new HashSet<NonFungibleToken>();
            AccountTransactions = new HashSet<AccountTransaction>();
        }

        public string Address { get; set; }
        public string Name { get; set; }
        public string SoulStaked { get; set; }
        public string Relay { get; set; } //todo test this relay

        public ICollection<FungibleBalance> TokenBalance { get; set; }
        public ICollection<NonFungibleToken> NonFungibleTokens { get; set; }
        public ICollection<AccountTransaction> AccountTransactions { get; }
    }
}
