using System;
using System.Collections.Generic;
using System.Linq;
using Phantasma.Blockchain;
using Phantasma.Core.Utils;
using Phantasma.Explorer.Infrastructure.Interfaces;

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

        public static BlockViewModel FromBlock(IRepository repository, Block block)
        {
            var chain = repository.NexusChain.FindChainForBlock(block);
            var transactions = chain.GetBlockTransactions(block);

            var vm = new BlockViewModel
            {
                Height = (int)block.Height,
                Timestamp = block.Timestamp,
                Transactions = transactions.Count(),
                Hash = block.Hash.ToString(),
                ParentHash = block.PreviousHash?.ToString(),
                MiningAddress = block.MinerAddress.Text,
                ChainName = chain.Name.ToTitleCase(),
                ChainAddress = chain.Address.Text,
                Reward = TokenUtils.ToDecimal(chain.GetBlockReward(block), Nexus.NativeTokenDecimals),
                Txs = new List<TransactionViewModel>()
            };
            var txsVm = transactions.Select(transaction => TransactionViewModel.FromTransaction(repository, vm, (Transaction)transaction)).ToList();

            vm.Txs = txsVm;
            return vm;
        }
    }
}
