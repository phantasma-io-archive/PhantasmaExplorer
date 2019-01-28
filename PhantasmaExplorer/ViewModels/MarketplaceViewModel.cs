using System.Collections.Generic;
using System.Linq;
using Phantasma.Blockchain.Tokens;
using Phantasma.RpcClient.DTOs;
using Token = Phantasma.Explorer.Domain.Entities.Token;

namespace Phantasma.Explorer.ViewModels
{
    public class MarketplaceViewModel
    {
        public List<AuctionViewModel> AuctionsList { get; set; } = new List<AuctionViewModel>();
        public int TotalAuctions => AuctionsList.Count;

        public static MarketplaceViewModel FromAuctionList(IList<AuctionDto> auctions, ICollection<Token> tokenList)
        {
            return new MarketplaceViewModel
            {
                AuctionsList = auctions.Select(p =>
                    AuctionViewModel.FromAuction(p, 
                        TokenUtils.ToDecimal(p.Price, 
                            (int)tokenList
                            .Single(c => c.Symbol.Equals(p.QuoteSymbol))
                            .Decimals)))
                    .ToList()
            };
        }
    }
}
