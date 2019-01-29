using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Phantasma.Explorer.Application.Queries;
using Phantasma.Explorer.Persistance;
using Phantasma.Explorer.ViewModels;
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

        public async Task<MarketplaceViewModel> GetAllAuctions()
        {
            var tokenQueries = new TokenQueries(_context);
            var tokenList = tokenQueries.QueryTokens();

            var auctions = await _phantasmaRpcService.GetAuctions.SendRequestAsync("NACHO"); //todo remove NACHO whn bug fixed

            return MarketplaceViewModel.FromAuctionList(auctions, tokenList);
        }
    }
}
