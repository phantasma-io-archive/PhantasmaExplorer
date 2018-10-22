using System.Collections.Generic;

namespace Phantasma.Explorer.ViewModels
{
    public class HomeViewModel
    {
        public List<BlockViewModel> Blocks { get; set; }
        public List<TransactionViewModel> Transactions { get; set; }
    }
}
