using System.Collections.Generic;

namespace Phantasma.Explorer.Domain.Entities
{
    public class Block
    {
        public Block()
        {
            Transactions = new HashSet<Transaction>();
        }

        public string Hash { get; set; }
        public string ChainAddress { get; set; }
        public string ChainName { get; set; }
        public string PreviousHash { get; set; }
        public uint Timestamp { get; set; }
        public uint Height { get; set; }
        public string Payload { get; set; }
        public string ValidatorAddress { get; set; }
        public string Reward { get; set; }

        public Chain Chain { get; set; }
        public ICollection<Transaction> Transactions { get; set; }
    }
}
