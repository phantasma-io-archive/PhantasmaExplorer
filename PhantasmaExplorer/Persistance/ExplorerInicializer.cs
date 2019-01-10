using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Internal;
using Phantasma.Explorer.Domain.Entities;
using Phantasma.RpcClient.Interfaces;

namespace Phantasma.Explorer.Persistance
{
    public class ExplorerInicializer
    {
        private IPhantasmaRpcService _phantasmaRpcService;
        public static async Task Initialize(ExplorerDbContext context)
        {
            var initializer = new ExplorerInicializer();
            await initializer.SeedEverythingAsync(context);
        }

        public async Task SeedEverythingAsync(ExplorerDbContext context)
        {
            context.Database.EnsureCreated();

            if (context.Chains.Any())
            {
                return; // Db has been seeded)
            }

            _phantasmaRpcService = (IPhantasmaRpcService)Explorer.AppServices.GetService(typeof(IPhantasmaRpcService));

            //var root = await _phantasmaRpcService.GetRootChain.SendRequestAsync();
            //var chains = await _phantasmaRpcService.GetChains.SendRequestAsync(); //name-address info only
            //var tokens = await _phantasmaRpcService.GetTokens.SendRequestAsync();


            await SeedApps(context);

            //Apps = appList;
            //_rootChain = root;

            //foreach (var token in tokens)
            //{
            //    _tokens.Add(token.Symbol, token);
            //}

            //// working

            //foreach (var chain in chains)
            //{
            //    var persistentChain = SetupChain(chain);
            //    var childs = chains.Where(p => p.ParentAddress.Equals(chain.Address));

            //    if (childs.Any())
            //    {
            //        persistentChain.AddChildren(childs.ToList());
            //    }

            //    await SetupBlocks(persistentChain);
            //}
        }

        private async Task SeedApps(ExplorerDbContext context)
        {
            var appList = await _phantasmaRpcService.GetApplications.SendRequestAsync();
            foreach (var dto in appList)
            {
                context.Apps.Add(new App
                {
                    Id = dto.Id,
                    Url = dto.Url,
                    Description = dto.Description,
                    Title = dto.Title,
                    Icon = dto.Icon
                });
            }

        }
    }
}
