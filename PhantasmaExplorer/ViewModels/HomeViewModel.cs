using System.Collections.Generic;

namespace Phantasma.Explorer.ViewModels
{
    public class HomeViewModel
    {
        public List<BlockViewModel> Blocks { get; set; }
        public List<TransactionViewModel> Transactions { get; set; }
        public Dictionary<string, uint> Chart { get; set; }
        public int TotalChains { get; set; }
        public int TotalTransactions { get; set; }
        public uint BlockHeight { get; set; }
        public decimal SOULUSD { get; set; }
        public decimal SOULBTC { get; set; }
        public decimal SOULETH { get; set; }
        public decimal SOULNEO { get; set; }
        public decimal MarketCap { get; set; }
    }
}
