using System;
using System.Collections.Generic;
using System.Linq;
using Phantasma.Core.Types;
using Phantasma.Explorer.Domain.Entities;
using Phantasma.Explorer.Persistance;
using Phantasma.Explorer.Utils;

namespace Phantasma.Explorer.ViewModels
{
    public class HomeViewModel
    {
        public int TotalChains { get; set; }
        public int TotalTransactions { get; set; }
        public uint BlockHeight { get; set; }

        public List<BlockHomeViewModel> Blocks { get; set; }
        public List<TransactionHomeViewModel> Transactions { get; set; }
        public Dictionary<string, uint> Chart { get; set; }
    }

    public class BlockHomeViewModel
    {
        public uint Height { get; set; }
        public string Hash { get; set; }
        public string ChainName { get; set; }
        public string ChainAddress { get; set; }
        public decimal Reward { get; set; }
        public string ValidatorAddress { get; set; }
        public int Transactions { get; set; }
        public DateTime Timestamp { get; set; }

        public static BlockHomeViewModel FromBlock(Block block)
        {
            return new BlockHomeViewModel
            {
                Height = block.Height,
                Hash = block.Hash,
                ChainName = block.ChainName,
                ChainAddress = block.ChainAddress,
                Reward = block.Reward,
                ValidatorAddress = block.ValidatorAddress,
                Transactions = block.Transactions.Count,
                Timestamp = new Timestamp(block.Timestamp),
            };
        }
    }

    public class TransactionHomeViewModel
    {
        public string Hash { get; set; }
        public DateTime Date { get; set; }
        public string ChainName { get; set; }
        public string ChainAddress { get; set; }
        public string Description { get; set; }

        public static TransactionHomeViewModel FromTransaction(Transaction tx)
        {
            return new TransactionHomeViewModel
            {
                Description = TransactionUtils.GetTxDescription(tx),
                ChainName = tx.Block.ChainName,
                ChainAddress = tx.Block.ChainAddress,
                Date = new Timestamp(tx.Timestamp),
                Hash = tx.Hash
            };
        }
    }
}
