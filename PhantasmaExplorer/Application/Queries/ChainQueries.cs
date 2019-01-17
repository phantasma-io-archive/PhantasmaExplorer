using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Phantasma.Explorer.Domain.Entities;
using Phantasma.Explorer.Persistance;
using Phantasma.RpcClient.DTOs;

namespace Phantasma.Explorer.Application.Queries
{
    public class ChainQueries
    {
        private readonly ExplorerDbContext _context;

        public ChainQueries(ExplorerDbContext context)
        {
            _context = context;
            _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        public int QueryChainCount => _context.Chains.Count();

        public ICollection<Chain> QueryChains()
        {
            return _context.Chains.ToList();
        }

        public ICollection<Chain> QueryChainIncludeBlocksAndTxs()
        {
            return _context.Chains.Include(p => p.Blocks).ThenInclude(p => p.Transactions).ToList();
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

        public IEnumerable<string> QueryChainNames()
        {
            return _context.Chains.Select(p => p.Name);
        }

        public string QueryChainName(string chainAddress)
        {
            return _context.Chains.SingleOrDefault(p => p.Address.Equals(chainAddress))?.Name;
        }

        public IEnumerable<ChainDto> QueryChainInfo() //todo remove dto
        {
            return _context.Chains.Select(p => new ChainDto
            {
                Address = p.Address,
                Name = p.Name,
                Height = p.Height,
                ParentAddress = p.ParentAddress
            });
        }
    }
}