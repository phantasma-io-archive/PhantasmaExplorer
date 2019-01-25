using System;
using System.Net;
using System.Net.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Phantasma.Explorer.Persistance;
using Phantasma.RpcClient;
using Phantasma.RpcClient.Interfaces;

namespace Phantasma.Explorer.Application
{
    public class AppServices
    {
        public IServiceProvider Services { get; set; }

        public AppServices(IServiceCollection serviceCollection)
        {
            ConfigureServices(serviceCollection);
            Services = serviceCollection.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddEntityFrameworkSqlite().AddDbContext<ExplorerDbContext>(options => options
               .UseSqlite("Data Source=PhantasmaExplorerDatabase.db;")
                , ServiceLifetime.Transient);

            serviceCollection.AddScoped<IPhantasmaRpcService>(provider => new PhantasmaRpcService(new RpcClient.Client.RpcClient(new Uri("http://localhost:7077/rpc"), httpClientHandler: new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            })));
        }
    }
}
