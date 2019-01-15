using System.Collections.Generic;

namespace Phantasma.Explorer.ViewModels
{
    public class HomeViewModel
    {
        public int TotalChains { get; set; }
        public int TotalTransactions { get; set; }
        public uint BlockHeight { get; set; }

        public List<BlockViewModel> Blocks { get; set; }
        public List<TransactionViewModel> Transactions { get; set; }
        public Dictionary<string, uint> Chart { get; set; }
    }
}
