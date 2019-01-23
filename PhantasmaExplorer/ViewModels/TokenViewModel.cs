using Phantasma.Blockchain.Tokens;
using Token = Phantasma.Explorer.Domain.Entities.Token;
using TokenFlags = Phantasma.Explorer.Domain.Entities.TokenFlags;

namespace Phantasma.Explorer.ViewModels
{
    public class TokenViewModel
    {
        public string Symbol { get; set; }
        public string Name { get; set; }
        public string LogoUrl { get; set; }
        public uint Decimals { get; set; }
        public decimal MaxSupply { get; set; }
        public decimal CurrentSupply { get; set; }
        public decimal Price { get; set; }
        public uint Transfers { get; set; }
        public TokenFlags Flags { get; set; }

        public static TokenViewModel FromToken(Token token, string logoUrl, decimal price = 0m)
        {
            return new TokenViewModel
            {
                Symbol = token.Symbol,
                Name = token.Name,
                MaxSupply = TokenUtils.ToDecimal(token.MaxSupply, (int)token.Decimals),
                CurrentSupply = TokenUtils.ToDecimal(token.CurrentSupply, (int)token.Decimals),
                Decimals = token.Decimals,
                Price = price,
                Transfers = token.TransactionCount,
                Flags = token.Flags,
                LogoUrl = logoUrl //todo
            };
        }

        public bool IsTransferable => Flags.HasFlag(TokenFlags.Transferable);
        public bool IsFungible => Flags.HasFlag(TokenFlags.Fungible);
        public bool IsFinite => Flags.HasFlag(TokenFlags.Finite);
    }
}
