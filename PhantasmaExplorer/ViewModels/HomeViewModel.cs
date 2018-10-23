using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Phantasma.Explorer.ViewModels
{
    public class HomeViewModel
    {
        public List<BlockViewModel> Blocks { get; set; }
        public List<TransactionViewModel> Transactions { get; set; }
        public Task SearchCommand { get; set; }
    }
}
