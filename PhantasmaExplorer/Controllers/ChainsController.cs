using System.Collections.Generic;
using System.Linq;
using Phantasma.Explorer.Application.Queries;
using Phantasma.Explorer.Persistance;
using Phantasma.Explorer.ViewModels;

namespace Phantasma.Explorer.Controllers
{
    public class ChainsController
    {
        private readonly ExplorerDbContext _context;

        public ChainsController(ExplorerDbContext context)
        {
            _context = context;
        }

        public List<ChainViewModel> GetChains()
        {
            var chainQuery = new ChainQueries(_context);
            var chainVmList = new List<ChainViewModel>();

            var allChains = chainQuery.QueryChains().ToList();

            foreach (var chain in allChains)
            {
                chainVmList.Add(ChainViewModel.FromChain(allChains, chain));
            }

            return chainVmList;
        }

        public ChainViewModel GetChain(string chainInput)
        {
            var chainQuery = new ChainQueries(_context);
            var repoChain = chainQuery.QueryChain(chainInput);
            var chainList = chainQuery.QueryChains().ToList();

            if (repoChain == null) return null;

            return ChainViewModel.FromChain(chainList, repoChain);
        }
    }
}