using System;
using System.Collections.Generic;
using System.Linq;
using Phantasma.Core.Types;
using Phantasma.Core.Utils;
using Phantasma.Explorer.Domain.Entities;

namespace Phantasma.Explorer.ViewModels
{
    public class BlockViewModel
    {
        public int Height { get; set; }
        public DateTime Timestamp { get; set; }
        public int Transactions { get; set; }
        public string Hash { get; set; }
        public string ParentHash { get; set; }
        public string ValidatorAddress { get; set; }
        public string ChainName { get; set; }
        public string ChainAddress { get; set; }
        public decimal Reward { get; set; }

        public List<TransactionViewModel> Txs { get; set; }

        public static BlockViewModel FromBlock(Block block)
        {
            var vm = new BlockViewModel
            {
                Height = (int) block.Height,
                Timestamp = new Timestamp(block.Timestamp),
                Transactions = block.Transactions.Count,
                Hash = block.Hash,
                ParentHash = block.PreviousHash,
                ValidatorAddress = block.ValidatorAddress,
                ChainName = block.ChainName.ToTitleCase(),
                ChainAddress = block.ChainAddress,
                Reward = block.Reward,
                Txs = block.Transactions.Select(TransactionViewModel.FromTransaction).ToList(),
            };


            return vm;
        }
    }

    public class BlockListViewModel
    {
        public DateTime Timestamp { get; set; }
        public int? Transactions { get; set; }
        public string Hash { get; set; }
        public string ChainName { get; set; }
        public string ChainAddress { get; set; }

        public static BlockListViewModel FromBlock(Block block, int? txsCount)
        {
            var vm = new BlockListViewModel
            {
                Timestamp = new Timestamp(block.Timestamp),
                Transactions = txsCount,
                Hash = block.Hash,
                ChainName = block.ChainName.ToTitleCase(),
                ChainAddress = block.ChainAddress,
            };

            return vm;
        }
    }
}