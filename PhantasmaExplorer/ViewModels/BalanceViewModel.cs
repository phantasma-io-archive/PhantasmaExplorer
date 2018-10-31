namespace Phantasma.Explorer.ViewModels
{
    public class BalanceViewModel
    {
        public string ChainName { get; set; }
        public decimal Balance { get; set; }
        public decimal Value { get; set; }
        public string Address { get; set; }
        public int TxnCount { get; set; }
        public TokenViewModel Token { get; set; } = new TokenViewModel();
    }
}
