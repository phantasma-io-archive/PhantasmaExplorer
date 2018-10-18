using System.Collections.Generic;
using Phantasma.Explorer.Infrastructure.Interfaces;
using Phantasma.Explorer.ViewModels;

namespace Phantasma.Explorer.Controllers
{
    public class BlocksController
    {
        public IRepository Repository { get; set; } //todo interface

        public BlocksController(IRepository repo)
        {
            Repository = repo;
        }

        public List<BlockViewModel> GetLatestBlocks()
        {
            var repoBlockList =  Repository.GetBlocks();
            var blockList = new List<BlockViewModel>();
            foreach (var block in repoBlockList)
            {
                blockList.Add(BlockViewModel.FromBlock(block));
            }

            return blockList;
        }

        public BlockViewModel GetBlock(string input)
        {
            var block = int.TryParse(input, out var height) ? Repository.GetBlock(height) : Repository.GetBlock(input);
            return BlockViewModel.FromBlock(block);
        }
    }
}