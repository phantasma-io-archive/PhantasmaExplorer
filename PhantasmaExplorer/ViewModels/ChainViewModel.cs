using System.Collections.Generic;
using System.Linq;
using Phantasma.Core.Utils;
using Phantasma.Explorer.Domain.Entities;

namespace Phantasma.Explorer.ViewModels
{
    public class ChainViewModel
    {
        public string Address { get; set; }
        public string Name { get; set; }
        public int Transactions { get; set; }
        public string ParentChain { get; set; }
        public uint Height { get; set; }

        public List<BlockViewModel> Blocks { get; set; }
        public Dictionary<string, string> ChildChains { get; set; }

        public static ChainViewModel FromChain(List<Chain> chains, Chain chain)
        {
            var lastBlocks = chain.Blocks.OrderByDescending(p => p.Timestamp).Take(20);
            var lastBlocksVm = lastBlocks.Select(BlockViewModel.FromBlock).ToList();

            var vm = new ChainViewModel
            {
                Address = chain.Address,
                Name = chain.Name.ToTitleCase(),
                Transactions = chain.Blocks.Select(p => p.Transactions.Count).Sum(),
                Height = chain.Height,
                Blocks = lastBlocksVm,
                ParentChain = chain.ParentAddress ?? ""
            };

            if (chains != null && chains.Any())
            {
                vm.ChildChains = new Dictionary<string, string>();

                foreach (var repoChain in chains)
                {
                    if (repoChain.ParentAddress.Equals(chain.Address))
                    {
                        vm.ChildChains[repoChain.Name] = repoChain.Address;
                    }
                }
            }

            return vm;
        }
    }
}