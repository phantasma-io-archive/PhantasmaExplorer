using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Phantasma.Explorer.Domain.Entities;
using Phantasma.Explorer.Persistance;

namespace Phantasma.Explorer.Application.Queries
{
    public class BlockQueries
    {
        private readonly ExplorerDbContext _context;

        public BlockQueries(ExplorerDbContext context)
        {
            _context = context;
            _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        public IEnumerable<Block> QueryBlocks(string chain = null, int amount = 20)
        {
            if (string.IsNullOrEmpty(chain)) //get last x blocks from all chains
            {
                return _context.Blocks.OrderByDescending(p => p.Timestamp).Take(amount);
            }

            return _context.Blocks
                .Where(p => p.Chain.Address.Equals(chain) || p.ChainAddress.Equals(chain))
                .OrderByDescending(p => p.Timestamp).Take(amount);
        }

        public Block QueryBlockForTransaction(string txHash)
        {
            return _context.Transactions.SingleOrDefault(p => p.Hash.Equals(txHash)).Block;
        }

        public Block QueryBlock(string blockHash)
        {
            return _context.Blocks.SingleOrDefault(p => p.Hash.Equals(blockHash));
        }

        public Block QueryBlock(int height, string chain)
        {
            return _context.Blocks
                .Where(p => p.Chain.Name.Equals(chain) || p.ChainAddress.Equals(chain))
                .SingleOrDefault(c => c.Height == height);
        }
    }
}
