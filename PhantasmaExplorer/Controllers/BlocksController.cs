using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Phantasma.Explorer.Application.Queries;
using Phantasma.Explorer.Persistance;
using Phantasma.Explorer.ViewModels;

namespace Phantasma.Explorer.Controllers
{
    public class BlocksController
    {
        public int GetBlocksCount(string chain = null)
        {
            var context = Explorer.AppServices.GetService<ExplorerDbContext>();
            var blockQuery = new BlockQueries(context);
            return blockQuery.QueryBlocksCount(chain);
        }

        public List<BlockViewModel> GetBlocks(int currentPage, int pageSize = 20, string chain = null)
        {
            var context = Explorer.AppServices.GetService<ExplorerDbContext>();

            var blockQuery = new BlockQueries(context);
            var query = blockQuery.QueryBlocks(chain).Skip((currentPage - 1) * pageSize).Take(pageSize);

            return query.AsEnumerable().Select(BlockViewModel.FromBlock).ToList();
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