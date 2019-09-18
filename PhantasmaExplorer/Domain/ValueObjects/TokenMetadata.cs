using System.Collections.Generic;
using Phantasma.Explorer.Domain.Infrastructure;

namespace Phantasma.Explorer.Domain.ValueObjects
{
    public class TokenMetadata : ValueObject
    {
        public string Key { get; set; }
        public string Value { get; set; }

        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return Key;
            yield return Value;
        }
    }
}
