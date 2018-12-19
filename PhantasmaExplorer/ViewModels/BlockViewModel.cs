using System;
using System.Collections.Generic;
using System.Linq;
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
        public string MiningAddress { get; set; }
        public string ChainName { get; set; }
        public string ChainAddress { get; set; }
        public decimal Reward { get; set; }
        public List<TransactionViewModel> Txs { get; set; }

        public static BlockViewModel FromBlock(IRepository repository, BlockDto block)
        {

            var vm = new BlockViewModel
            {
                Height = (int)block.Height,
                Timestamp = DateTime.Parse(block.Timestamp),
                Transactions = block.Txs.Count,
                Hash = block.Hash,
                ParentHash = block.PreviousHash,
                MiningAddress = Cryptography.Address.Null.Text, // block.MinerAddress.Text, TODO fixme later
                //ChainName = bloc.Name.ToTitleCase(), todo
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
