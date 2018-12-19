using System.Collections.Generic;
using System.Linq;
using Phantasma.Cryptography;
using Phantasma.RpcClient.DTOs;

namespace Phantasma.Explorer.Infrastructure.Models
{
    public class ChainRepository //todo revisit
    {
        public ChainDto ChainInfo { get; set; }
        public Dictionary<Hash, BlockDto> Blocks { get; set; }

        public ChainRepository(ChainDto chain, Dictionary<Hash, BlockDto> blocks)
        {
            ChainInfo = chain;
            Blocks = blocks;
        }

        public List<BlockDto> GetBlocks => Blocks.Values.ToList();
    }
}
