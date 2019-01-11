using System.Collections.Generic;
using Phantasma.Explorer.Domain.ValueObjects;

namespace Phantasma.Explorer.Domain.Entities
{
    public class Transaction
    {
        public Transaction()
        {
            Events = new List<Event>();
        }

        public string Hash { get; set; }
        public uint Timestamp { get; set; }
        public string Script { get; set; }

        public Chain Chain { get; set; }
        public Block Block { get; set; }
        public ICollection<Event> Events { get; set; }
    }
}
