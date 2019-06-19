using System.Collections.Generic;
using System.Linq;
using Phantasma.Numerics;
using Phantasma.RpcClient.DTOs;
using Token = Phantasma.Explorer.Domain.Entities.Token;

namespace Phantasma.Explorer.ViewModels
{
    public class MarketplaceViewModel
    {
        public List<AuctionViewModel> AuctionsList { get; set; } = new List<AuctionViewModel>();
        public int TotalAuctions => AuctionsList.Count;
        public string Chain;

        public static MarketplaceViewModel FromAuctionList(IList<AuctionDto> auctions, ICollection<Token> tokenList)
        {
            var auctionsList = new List<AuctionViewModel>();

            foreach (var auctionDto in auctions)
            {
                var token = tokenList.Single(p => p.Symbol.Equals(auctionDto.BaseSymbol));
                var quoteToken = tokenList.Single(p => p.Symbol.Equals(auctionDto.QuoteSymbol));
                var price = UnitConversion.ToDecimal(auctionDto.Price, (int)quoteToken.Decimals);

                var auctionViewModel = AuctionViewModel.FromAuction(auctionDto, price, GetMetadata(token, "viewer"),
                    GetMetadata(token, "details"));

                auctionsList.Add(auctionViewModel);
            }

            return new MarketplaceViewModel { AuctionsList = auctionsList };
        }

        private static string GetMetadata(Token token, string key)
        {
            var meta = token.MetadataList.SingleOrDefault(p => p.Key.Equals(key))?.Value;

            if (meta == null)
            {
                return "";
            }
            else
            {
                return System.Text.Encoding.UTF8.GetString(meta).Trim('*');
            }
        }
    }
}
