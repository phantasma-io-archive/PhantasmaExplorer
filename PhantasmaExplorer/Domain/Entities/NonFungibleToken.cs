namespace Phantasma.Explorer.Domain.Entities
{
    public class NonFungibleToken
    {
        public string Id { get; set; }
        public string Chain { get; set; } //todo change ChainName to address
        public string TokenSymbol { get; set; }
        public string AccountAddress { get; set; }
        public string ViewerUrl { get; set; }
        public string DetailsUrl { get; set; }

        public Account Account { get; set; }
    }
}
