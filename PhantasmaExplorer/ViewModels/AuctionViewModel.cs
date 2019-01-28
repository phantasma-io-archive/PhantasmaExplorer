using System;
using Phantasma.Core.Types;
using Phantasma.RpcClient.DTOs;

namespace Phantasma.Explorer.ViewModels
{
    public class AuctionViewModel
    {
        public string CreatorAddress;
        public string QuoteSymbol;
        public string BaseSymbol;
        public string TokenId;
        public decimal Price;
        public DateTime StartDate;
        public DateTime EndDate;

        public static AuctionViewModel FromAuction(AuctionDto auction, decimal calculatedPrice)
        {
            return new AuctionViewModel
            {
                BaseSymbol = auction.BaseSymbol,
                QuoteSymbol = auction.QuoteSymbol,
                CreatorAddress = auction.CreatorAddress,
                Price = calculatedPrice,
                EndDate = new Timestamp(auction.StartDate),
                StartDate = new Timestamp(auction.StartDate),
                TokenId = auction.TokenId
            };
        }
    }
}
