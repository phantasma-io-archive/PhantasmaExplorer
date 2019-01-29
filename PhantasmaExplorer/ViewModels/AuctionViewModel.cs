using System;
using Phantasma.Core.Types;
using Phantasma.Explorer.Domain.Entities;
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

        public NftViewModel NftVm { get; set; }

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
