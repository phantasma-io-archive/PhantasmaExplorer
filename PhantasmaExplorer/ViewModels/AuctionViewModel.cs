using System;
using Phantasma.Core.Types;
using Phantasma.RpcClient.DTOs;

namespace Phantasma.Explorer.ViewModels
{
    public class AuctionViewModel
    {
        public string CreatorAddress { get; set; }
        public string QuoteSymbol { get; set; }
        public string BaseSymbol { get; set; }
        public string TokenId { get; set; }
        public decimal Price { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public string ViewerUrl { get; set; }
        public string InfoUrl { get; set; }

        public static AuctionViewModel FromAuction(AuctionDto auction, decimal calculatedPrice, string viewerUrl, string infoUrl)
        {
            var vm = new AuctionViewModel
            {
                BaseSymbol = auction.BaseSymbol,
                QuoteSymbol = auction.QuoteSymbol,
                CreatorAddress = auction.CreatorAddress,
                Price = calculatedPrice,
                EndDate = new Timestamp(auction.StartDate),
                StartDate = new Timestamp(auction.StartDate),
                TokenId = auction.TokenId,
                InfoUrl = infoUrl
            };

            if (string.IsNullOrEmpty(viewerUrl)) vm.ViewerUrl = "/img/stock_nft.png";
            else vm.ViewerUrl = viewerUrl + vm.TokenId;

            return vm;
        }
    }
}
