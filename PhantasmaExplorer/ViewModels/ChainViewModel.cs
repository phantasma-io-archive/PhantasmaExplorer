using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Internal;
using Phantasma.Core;
using Phantasma.Core.Utils;
using Phantasma.Explorer.Domain.Entities;
using Phantasma.Explorer.Utils;

namespace Phantasma.Explorer.ViewModels
{
    public class ChainViewModel
    {
        public string Address { get; set; }
        public string Name { get; set; }
        public int Transactions { get; set; }
        public string ParentChain { get; set; }
        public uint Height { get; set; }
        public string Contracts { get; set; }

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
                Contracts = chain.Contracts.Join("; "),
                ParentChain = chain.ParentAddress ?? "",
                ChildChains = ChainUtils.SetupChainChildren(chains, chain.Address)
            };

            return vm;
        }
    }

    public class SimpleChainViewModel
    {
        public string Address { get; set; }
        public string Name { get; set; }
        public int Transactions { get; set; }
        public string ParentChain { get; set; }
        public uint Height { get; set; }

        public static Expression<Func<Chain, SimpleChainViewModel>> Projection
        {
            get
            {
                return x => new SimpleChainViewModel()
                {
                    Address = x.Address,
                    Name = x.Name,
                    ParentChain = x.ParentAddress ?? "",
                    Height = x.Height,
                    Transactions = x.Blocks.Select(p => p.Transactions.Count).Sum()
                };
            }
        }
    }
}