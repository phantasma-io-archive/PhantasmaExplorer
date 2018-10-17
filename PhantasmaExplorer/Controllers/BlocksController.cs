using System.Collections.Generic;
using System.Linq;
using Phantasma.Blockchain;
using Phantasma.Cryptography;
using Phantasma.Explorer.ViewModels;

namespace Phantasma.Explorer.Controllers
{
    public class BlocksController
    {
        public Nexus NexusChain { get; set; } //todo this should be replace with a repository or db instance

        public BlocksController(Nexus chain)
        {
            NexusChain = chain;
        }

        public List<BlockViewModel> GetLatestBlock()
        {
            List<Block> tempList = new List<Block>();

            var blocksList = new List<BlockViewModel>();

            foreach (var chain in NexusChain.Chains)
            {
                if (chain.Blocks.Any())
                {
                    tempList.AddRange(chain.Blocks.TakeLast(20));
                }
            }

            tempList = tempList.OrderBy(block => block.Timestamp.Value).ToList();
            foreach (var block in tempList)
            {
                blocksList.Add(BlockViewModel.FromBlock(block));
            }

            return blocksList;
        }

        public BlockViewModel GetBlock(string input)
        {
            if (int.TryParse(input, out var height))
            {
                return GetBlockByHeight(height);
            }

            return GetBlockByHash(input);
        }

        private BlockViewModel GetBlockByHash(string hash)
        {
            var blockHash = (Hash.Parse(hash));
            foreach (var chain in NexusChain.Chains)
            {
                var block = chain.FindBlock(blockHash);
                if (block != null)
                {
                    return BlockViewModel.FromBlock(block);
                }
            }

            return null;
        }

        private BlockViewModel GetBlockByHeight(int height)
        {
            var block = NexusChain.RootChain.FindBlock(height);
            if (block != null)
            {
                return BlockViewModel.FromBlock(block);
            }

            return null;
        }
    }
}