using System.Collections.Generic;
using Phantasma.Explorer.Domain.ValueObjects;

namespace Phantasma.Explorer.Domain.Entities
{
    public class Account
    {
        public Account()
        {
            TokenBalance = new HashSet<FBalance>();
            NonFungibleTokens = new HashSet<NonFungibleToken>();
            AccountTransactions = new HashSet<AccountTransaction>();
        }

        public string Address { get; set; }
        public string Name { get; set; }

        public ICollection<FBalance> TokenBalance { get; }
        public ICollection<NonFungibleToken> NonFungibleTokens { get; set; }
        public ICollection<AccountTransaction> AccountTransactions { get; }
    }
}
