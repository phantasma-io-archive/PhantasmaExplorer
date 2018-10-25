using System;
using System.Collections.Generic;
using System.Linq;
using Phantasma.Blockchain;
using Phantasma.Core.Utils;

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
        public List<TransactionViewModel> Txs { get; set; }

        public static BlockViewModel FromBlock(Block block)
        {
            var vm = new BlockViewModel
            {
                Height = (int)block.Height,
                Timestamp = block.Timestamp,
                Transactions = block.Transactions.Count(),
                Hash = block.Hash.ToString(),
                ParentHash = block.PreviousHash?.ToString(),
                MiningAddress = block.MinerAddress.Text,
                ChainName = block.Chain.Name.ToTitleCase(),
                ChainAddress = block.Chain.Address.Text,
                Txs = new List<TransactionViewModel>()
            };
            var txsVm = block.Transactions.Select(transaction => TransactionViewModel.FromTransaction(vm, (Transaction) transaction, null)).ToList();

            vm.Txs = txsVm;
            return vm;
        }
    }
}
