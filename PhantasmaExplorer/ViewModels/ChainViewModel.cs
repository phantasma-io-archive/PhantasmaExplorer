using System.Collections.Generic;
using System.Linq;
using Phantasma.Blockchain;
using Phantasma.Core.Utils;

namespace Phantasma.Explorer.ViewModels
{
    public class ChainViewModel
    {
        public string Address { get; set; }
        public string Name { get; set; }
        public int Transactions { get; set; }
        public string ParentChain { get; set; }
        public int Height { get; set; }
        public List<BlockViewModel> Blocks { get; set; }
        public Dictionary<string, string> ChildChains { get; set; }


        public static ChainViewModel FromChain(Chain chain, List<BlockViewModel> lastBlocks)
        {
            var vm = new ChainViewModel
            {
                Address = chain.Address.Text,
                Name = chain.Name.ToTitleCase(),
                Transactions = chain.TransactionCount,
                Height = chain.Blocks.Count(),
                Blocks = lastBlocks,
                ParentChain = chain.ParentChain?.Address.Text ?? ""
            };

            if (chain.ChildChains.Any())
            {
                vm.ChildChains = new Dictionary<string, string>();
                foreach (var childChain in chain.ChildChains)
                {
                    vm.ChildChains[childChain.Name] = childChain.Address.Text;
                }
            }
            return vm;
        }
    }
}