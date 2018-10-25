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
        public CoinRateViewModel SOULUSD { get; set; }
        public CoinRateViewModel SOULBTC { get; set; }
        public CoinRateViewModel SOULETH { get; set; }
        public CoinRateViewModel SOULNEO { get; set; }
        public decimal MarketCap { get; set; }
    }
}
