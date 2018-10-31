using Phantasma.Blockchain;
using Phantasma.Blockchain.Tokens;

namespace Phantasma.Explorer.ViewModels
{
    public class TokenViewModel
    {
        public string Symbol { get; set; }
        public string Name { get; set; }
        public string LogoUrl { get; set; }
        public string Description { get; set; }
        public int Decimals { get; set; }
        public decimal MaxSupply { get; set; }
        public decimal CurrentSupply { get; set; }
        public decimal Price { get; set; }
        public int Transfers { get; set; }
        public TokenFlags Flags { get; set; }

        public static TokenViewModel FromToken(Token token, string logoUrl, int transfers, decimal price = 0m)
        {
            return new TokenViewModel
            {
                Symbol = token.Symbol,
                Name = token.Name,
                MaxSupply = TokenUtils.ToDecimal(token.MaxSupply, token.Decimals),
                CurrentSupply = TokenUtils.ToDecimal(token.CurrentSupply, token.Decimals),
                Decimals = (int)token.Decimals,
                Price = price,
                Transfers = transfers,
                Flags = token.Flags,
                LogoUrl = logoUrl //todo
            };
        }

        public bool IsTransferable => Flags.HasFlag(TokenFlags.Transferable);
        public bool IsFungible => Flags.HasFlag(TokenFlags.Fungible);
        public bool IsFinite => Flags.HasFlag(TokenFlags.Finite);
    }
}
