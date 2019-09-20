using System.Collections.Generic;
using Phantasma.Explorer.Domain.Infrastructure;

namespace Phantasma.Explorer.Domain.ValueObjects
{
    public class FungibleBalance : ValueObject
    {
        public string Amount { get; set; }
        public string TokenSymbol { get; set; }
        public string Chain { get; set; }

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return Amount;
            yield return TokenSymbol;
            yield return Chain;
        }
    }
}
