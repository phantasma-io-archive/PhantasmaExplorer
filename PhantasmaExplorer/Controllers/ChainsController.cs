using System.Collections.Generic;
using Phantasma.Explorer.Infrastructure.Interfaces;
using Phantasma.Explorer.ViewModels;

namespace Phantasma.Explorer.Controllers
{
    public class ChainsController
    {
        private IRepository Repository { get; }

        public ChainsController(IRepository repo)
        {
            Repository = repo;
        }

        public List<ChainViewModel> GetChains()
        {
            var repoChains = Repository.GetAllChains();
            var chainList = new List<ChainViewModel>();

            foreach (var repoChain in repoChains)
            {
                var blockList = new List<BlockViewModel>();
                var lastBlocks = Repository.GetBlocks(repoChain.Address);

                foreach (var block in lastBlocks)
                {
                    blockList.Add(BlockViewModel.FromBlock(Repository, block));
                }

                chainList.Add(ChainViewModel.FromChain(Repository, repoChain, blockList));
            }

            return chainList;
        }

        public ChainViewModel GetChain(string chainInput)
        {
            var repoChain = Repository.GetChain(chainInput);

            if (repoChain == null)
            {
                repoChain = Repository.GetChain(chainInput);
                if (repoChain == null) return null;
            }

            var blockList = new List<BlockViewModel>();
            var lastBlocks = Repository.GetBlocks(repoChain.ChainInfo.Address);

            foreach (var block in lastBlocks)
            {
                blockList.Add(BlockViewModel.FromBlock(Repository, block));
            }

            return ChainViewModel.FromChain(Repository, repoChain.ChainInfo, blockList);
        }
    }
}