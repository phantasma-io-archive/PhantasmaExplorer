using Phantasma.Explorer.Domain.Infrastructure;
using System.Collections.Generic;

namespace Phantasma.Explorer.Domain.ValueObjects
{
    public class Interop : ValueObject
    {
        public string Platform { get; set; }
        public string Address { get; set; }
        public string InteropAddress { get; set; }

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return Platform;
            yield return Address;
            yield return InteropAddress;
        }
    }
}
