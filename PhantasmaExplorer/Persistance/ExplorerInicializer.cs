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
            _phantasmaRpcService = (IPhantasmaRpcService)Explorer.AppServices.GetService(typeof(IPhantasmaRpcService));

            if (!context.Apps.Any())
            {
                await SeedApps(context);
            }
            if (!context.Tokens.Any())
            {
                await SeedTokens(context);
            }

            if (!context.Chains.Any())
            {
                await SeedChains(context);
            }


            //var root = await _phantasmaRpcService.GetRootChain.SendRequestAsync();
            //var chains = await _phantasmaRpcService.GetChains.SendRequestAsync(); //name-address info only
            //var tokens = await _phantasmaRpcService.GetTokens.SendRequestAsync();

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

        private async Task SeedChains(ExplorerDbContext context)
        {
            var chains = await _phantasmaRpcService.GetChains.SendRequestAsync();

            foreach (var chainDto in chains)
            {
                context.Chains.Add(new Chain()
                {
                    Address = chainDto.Address,
                    Name = chainDto.Name,
                    Height = chainDto.Height,
                    ParentAddress = chainDto.ParentAddress,
                });
            }

            await context.SaveChangesAsync();
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

            await context.SaveChangesAsync();
        }

        private async Task SeedTokens(ExplorerDbContext context)
        {
            var tokenList = await _phantasmaRpcService.GetTokens.SendRequestAsync();

            foreach (var tokenDto in tokenList)
            {
                context.Tokens.Add(new Token
                {
                    Name = tokenDto.Name,
                    Symbol = tokenDto.Symbol,
                    Decimals = (uint)tokenDto.Decimals,
                    Flags = (TokenFlags)tokenDto.Flags,
                    MaxSupply = tokenDto.MaxSupply,
                    CurrentSupply = tokenDto.CurrentSupply,
                    Fungible = tokenDto.Fungible,
                    OwnerAddress = tokenDto.OwnerAddress
                });
            }

            await context.SaveChangesAsync();
        }
    }
}
