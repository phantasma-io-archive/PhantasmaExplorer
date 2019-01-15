﻿using System.Collections.Generic;
using System.Linq;
using Phantasma.Explorer.Application.Queries;
using Phantasma.Explorer.ViewModels;

namespace Phantasma.Explorer.Controllers
{
    public class ChainsController
    {
        public List<ChainViewModel> GetChains()
        {
            var chainQuery = new ChainQueries();
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
            var chainQuery = new ChainQueries();
            var repoChain = chainQuery.QueryChain(chainInput);
            var chainList = chainQuery.QueryChains().ToList();

            if (repoChain == null) return null;

            return ChainViewModel.FromChain(chainList, repoChain);
        }
    }
}