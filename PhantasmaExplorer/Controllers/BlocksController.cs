using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Phantasma.Explorer.Application.Queries;
using Phantasma.Explorer.Persistance;
using Phantasma.Explorer.ViewModels;

namespace Phantasma.Explorer.Controllers
{
    public class BlocksController
    {
        public List<BlockViewModel> GetLatestBlocks()
        {
            var context = Explorer.AppServices.GetService<ExplorerDbContext>();

            var blockQuery = new BlockQueries(context);
            var blockList = new List<BlockViewModel>();

            foreach (var block in blockQuery.QueryBlocks())
            {
                blockList.Add(BlockViewModel.FromBlock(block));
            }

            return blockList;
        }

        public BlockViewModel GetBlock(string input)
        {
            var context = Explorer.AppServices.GetService<ExplorerDbContext>();

            var blockQuery = new BlockQueries(context);

            var block = int.TryParse(input, out var height)
                ? blockQuery.QueryBlock(height, "main")
                : blockQuery.QueryBlock(input);//todo height only works with main

            if (block != null)
            {
                return BlockViewModel.FromBlock(block);
            }

            return null;
        }
    }
}