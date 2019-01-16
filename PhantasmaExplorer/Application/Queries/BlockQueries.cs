using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Phantasma.Explorer.Domain.Entities;
using Phantasma.Explorer.Persistance;
using Phantasma.Explorer.Utils;

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

        public ICollection<Block> QueryBlocks(string chain = null, int amount = 20)
        {
            if (string.IsNullOrEmpty(chain)) //get last x blocks from all chains
            {
                return _context.Blocks
                    .OrderByDescending(p => p.Timestamp)
                    .Take(amount)
                    .IncludeTransactions()
                    .ToList();
            }

            return _context.Blocks
                .Where(p => p.Chain.Address.Equals(chain) || p.ChainAddress.Equals(chain))
                .OrderByDescending(p => p.Height)
                .Take(amount)
                .IncludeTransactions()
                .ToList();
        }

        public Block QueryBlock(string blockHash)
        {
            return _context.Blocks
                .Include(p => p.Transactions)
                .SingleOrDefault(p => p.Hash.Equals(blockHash));
        }

        public Block QueryBlock(int height, string chain)
        {
            return _context.Blocks
                .Where(p => p.Chain.Name.Equals(chain) || p.ChainAddress.Equals(chain))
                .SingleOrDefault(c => c.Height == height);
        }
    }
}
