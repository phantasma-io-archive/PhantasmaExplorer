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
        }

        public IQueryable<Block> QueryBlocks(string chain)
        {
            var query = _context.Blocks.OrderByDescending(p => p.Timestamp);
            if (!string.IsNullOrEmpty(chain))
            {
                return query.Where(p => p.ChainAddress.Equals(chain) || p.ChainName.Equals(chain))
                    .Select(p => new Block
                    {
                        ChainAddress = p.ChainAddress,
                        ChainName = p.ChainName,
                        Hash = p.Hash,
                        Timestamp = p.Timestamp
                    });
            }

            return query.Select(p => new Block
            {
                ChainAddress = p.ChainAddress,
                ChainName = p.ChainName,
                Hash = p.Hash,
                Timestamp = p.Timestamp
            });
        }

        public ICollection<Block> QueryLastBlocks(string chain = null, int amount = 20)
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
                .Where(p => p.ChainAddress.Equals(chain) || p.ChainName.Equals(chain))
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
                .Where(p => p.ChainName.Equals(chain) || p.ChainAddress.Equals(chain))
                .SingleOrDefault(c => c.Height == height);
        }

        public int QueryBlocksCount(string chain)
        {
            return string.IsNullOrEmpty(chain)
                ? _context.Blocks.Count()
                : _context.Blocks.Count(p => p.ChainName.Equals(chain) || p.ChainAddress.Equals(chain));
        }

        public int? QueryBlockTxsCount(string blockHash)
        {
            return _context.Blocks.IncludeTransactions().SingleOrDefault(p => p.Hash.Equals(blockHash))?.Transactions.Count;
        }
    }
}
