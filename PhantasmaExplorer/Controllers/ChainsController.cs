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
            var repoChains = Repository.GetAllChainsInfo();
            var chainList = new List<ChainViewModel>();

            foreach (var repoChain in repoChains)
            {
                var blockList = new List<BlockViewModel>();
                var lastBlocks = Repository.GetBlocks(repoChain.Address);

                foreach (var block in lastBlocks)
                {
                    blockList.Add(BlockViewModel.FromBlock(Repository, block));
                }

                var totalTx = Repository.GetTotalChainTransactionCount(repoChain.Address);
                chainList.Add(ChainViewModel.FromChain(repoChain, blockList, totalTx));
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
            var lastBlocks = Repository.GetBlocks(repoChain.Address);

            foreach (var block in lastBlocks)
            {
                blockList.Add(BlockViewModel.FromBlock(Repository, block));
            }

            var totalTxs = Repository.GetTotalChainTransactionCount(repoChain.Address);
            return ChainViewModel.FromChain(repoChain.GetChainInfo, blockList, totalTxs);
        }
    }
}