using System.Collections.Generic;

namespace Phantasma.Explorer.Domain.Entities
{
    public class NFBalance
    {
        public NFBalance()
        {
            Tokens = new List<NonFungibleToken>();
        }

        public ICollection<NonFungibleToken> Tokens { get; set; }
    }
}
