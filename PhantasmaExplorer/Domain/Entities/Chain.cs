using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Phantasma.Explorer.Domain.Entities
{
    public class Chain
    {
        public Chain()
        {
            Blocks = new HashSet<Block>();
        }

        public string Name { get; set; }
        public string Address { get; set; }
        public string ParentAddress { get; set; }
        public uint Height { get; set; }


        public string[] Contracts { get; set; }

        public ICollection<Block> Blocks { get; }
    }
}
