using System;
using System.Collections.Generic;
using System.Linq;
using Phantasma.Core.Types;
using Phantasma.Core.Utils;
using Phantasma.Explorer.Infrastructure.Interfaces;
using Phantasma.RpcClient.DTOs;

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

        public static BlockViewModel FromBlock(IRepository repository, BlockDto block)
        {
            var vm = new BlockViewModel
            {
                Height = (int)block.Height,
                Timestamp = new Timestamp((uint)block.Timestamp),
                Transactions = block.Txs.Count,
                Hash = block.Hash,
                ParentHash = block.PreviousHash,
                ValidatorAddress = block.ValidatorAddress,
                ChainName = repository.GetChainName(block.ChainAddress).ToTitleCase(),
                ChainAddress = block.ChainAddress,
                Reward = block.Reward,
                Txs = new List<TransactionViewModel>()
            };

            var txsVm = block.Txs.Select(transaction => TransactionViewModel.FromTransaction(repository, vm, transaction)).ToList();

            vm.Txs = txsVm;
            return vm;
        }
    }
}