using System.Collections.Generic;

namespace Phantasma.Explorer.ViewModels
{
    public class CoinRateViewModel
    {
        public string Symbol { get; set; }
        public decimal Rate { get; set; }
        public decimal ChangePercentage { get; set; }
        public Dictionary<string, decimal> Chart { get; set; }
    }
}
