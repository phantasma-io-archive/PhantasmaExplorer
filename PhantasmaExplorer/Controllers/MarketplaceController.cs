using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Phantasma.Explorer.Application;
using Phantasma.Explorer.Application.Queries;
using Phantasma.Explorer.Persistance;
using Phantasma.Explorer.ViewModels;
using Phantasma.RpcClient.DTOs;
using Phantasma.RpcClient.Interfaces;

namespace Phantasma.Explorer.Controllers
{
    public class MarketplaceController : BaseController
    {
        private readonly IPhantasmaRpcService _phantasmaRpcService;

        public MarketplaceController() : base(Explorer.AppServices.GetService<ExplorerDbContext>())
        {
            _phantasmaRpcService = Explorer.AppServices.GetService<IPhantasmaRpcService>();
        }

        public async Task<MarketplaceViewModel> GetAuctions(string chain, int currentPage, int pageSize = AppSettings.PageSize, string tokenSymbol = null)
        {
            var tokenQueries = new TokenQueries(_context);
            var chainQueries = new ChainQueries(_context);
            var tokenList = tokenQueries.QueryTokens();

            var chains = chainQueries.QueryChains();
            List<AuctionDto> auctions = new List<AuctionDto>();

            var auction = await _phantasmaRpcService.GetAuctions.SendRequestAsync(chain, "", currentPage, pageSize);
            auctions.AddRange(auction.AuctionsList);

            return MarketplaceViewModel.FromAuctionList(auctions, tokenList);
        }

        public async Task<int> GetAuctionsCount(string chain = null)
        {
            return await _phantasmaRpcService.GetAuctionCount.SendRequestAsync(chain);
        }

        public async Task<List<string>> GetChainsWithMarketsAndActiveAuctions()
        {
            var chainQueries = new ChainQueries(_context);
            var chainsWithMarket = chainQueries.QueryChains()
                .Where(p => p.Contracts.Any(c => c == "market"))
                .Select(p => p.Name).ToList();

            List<string> chainList = new List<string>();

            foreach (var chain in chainsWithMarket) //check if they have auctions
            {
                var auctions = await _phantasmaRpcService.GetAuctionCount.SendRequestAsync(chain);
                if (auctions > 0) chainList.Add(chain);
            }

            return chainList;
        }
    }
}
