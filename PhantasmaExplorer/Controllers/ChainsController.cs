using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Phantasma.Explorer.Application.Queries;
using Phantasma.Explorer.Persistance;
using Phantasma.Explorer.ViewModels;

namespace Phantasma.Explorer.Controllers
{
    public class ChainsController
    {
        public List<SimpleChainViewModel> GetChains()
        {
            var context = Explorer.AppServices.GetService<ExplorerDbContext>();

            var chainQuery = new ChainQueries(context);
            var chainVmList = new List<SimpleChainViewModel>();

            var allChains = chainQuery.SimpleQueryChains().ToList();

            foreach (var chain in allChains)
            {
                chainVmList.Add(chain);
            }

            return chainVmList;
        }

        public ChainViewModel GetChain(string chainInput)
        {
            var context = Explorer.AppServices.GetService<ExplorerDbContext>();

            var chainQuery = new ChainQueries(context);
            var repoChain = chainQuery.QueryChainIncludeBlocksAndTxs(chainInput);
            var chainList = chainQuery.QueryChains().ToList();

            if (repoChain == null) return null;

            return ChainViewModel.FromChain(chainList, repoChain);
        }
    }
}