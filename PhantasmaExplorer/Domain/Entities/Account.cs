using System.Collections.Generic;
using Phantasma.Explorer.Domain.ValueObjects;

namespace Phantasma.Explorer.Domain.Entities
{
    public class Account
    {
        public Account()
        {
            FTokenBalance = new HashSet<FBalance>();
            NFTokenBalance = new HashSet<NFBalance>();
            Transactions = new HashSet<Transaction>();
        }

        public string Address { get; set; }
        public string Name { get; set; }

        public ICollection<FBalance> FTokenBalance { get; }
        public ICollection<NFBalance> NFTokenBalance { get; }
        public ICollection<Transaction> Transactions { get; }
    }
}
