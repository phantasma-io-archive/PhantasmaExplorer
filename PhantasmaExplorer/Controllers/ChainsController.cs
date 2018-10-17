using System.Collections.Generic;
using System.Linq;
using Phantasma.Blockchain;
using Phantasma.Cryptography;
using Phantasma.Explorer.ViewModels;

namespace Phantasma.Explorer.Controllers
{
    public class ChainsController
    {
        public Nexus NexusChain { get; set; } //todo this should be replace with a repository or db instance

        public ChainsController(Nexus chain)
        {
            NexusChain = chain;
        }

        public List<ChainViewModel> GetChains()
        {
            var chainList = new List<ChainViewModel>();
            foreach (var chain in NexusChain.Chains)
            {
                chainList.Add(ChainViewModel.FromChain(chain, null));
            }

            return chainList;
        }

        public ChainViewModel GetChain(string chainInput)
        {
            var chainAddress = Address.FromText(chainInput);
            var chain = NexusChain.Chains.SingleOrDefault(c => c.Address == chainAddress);

            if (chain != null)
            {
                List<BlockViewModel> lastBlocks = new List<BlockViewModel>();
                var blocks = chain.Blocks.ToList().TakeLast(20);
                foreach (var block in blocks)
                {
                    lastBlocks.Add(BlockViewModel.FromBlock(block));
                }

                return ChainViewModel.FromChain(chain, lastBlocks);
            }

            return null;
        }
    }
}