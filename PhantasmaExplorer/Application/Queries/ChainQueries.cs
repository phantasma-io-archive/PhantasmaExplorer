using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Phantasma.Explorer.Domain.Entities;
using Phantasma.Explorer.Persistance;
using Phantasma.Explorer.ViewModels;

namespace Phantasma.Explorer.Application.Queries
{
    public class ChainQueries
    {
        private readonly ExplorerDbContext _context;

        public ChainQueries(ExplorerDbContext context)
        {
            _context = context;
        }

        public int QueryChainCount => _context.Chains.Count();

        public ICollection<Chain> QueryChains()
        {
            return _context.Chains.ToList();
        }

        public ICollection<SimpleChainViewModel> SimpleQueryChains()
        {
            return _context.Chains.Select(SimpleChainViewModel.Projection).ToList();
        }

        public Chain QueryChain(string input)
        {
            return _context.Chains
                .Include(p => p.Blocks)
                .SingleOrDefault(p => p.Address.Equals(input) || p.Name.Equals(input));
        }

        public Chain QueryChainIncludeBlocksAndTxs(string input)
        {
            return _context.Chains
                .Include(p => p.Blocks)
                .ThenInclude(p => p.Transactions)
                .SingleOrDefault(p => p.Address.Equals(input) || p.Name.Equals(input));
        }
    }
}