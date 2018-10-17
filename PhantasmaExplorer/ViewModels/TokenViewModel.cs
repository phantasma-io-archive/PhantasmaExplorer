namespace Phantasma.Explorer.ViewModels
{
   public class TokenViewModel
    {
        public string Symbol { get; set; }
        public string Name { get; set; }
        public string LogoUrl { get; set; }
        public string Description { get; set; }
        public string ContractHash { get; set; }
        public int Decimals { get; set; }
        public decimal MaxSupply { get; set; }
        public decimal CurrentSupply { get; set; }
    }
}
