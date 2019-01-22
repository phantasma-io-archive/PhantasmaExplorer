using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Phantasma.Explorer.Application;
using Phantasma.Explorer.Persistance;
using Phantasma.Explorer.Site;

namespace Phantasma.Explorer
{
    public class Explorer
    {
        public static IServiceProvider AppServices => _app.Services;
        private static AppServices _app;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Initializing Phantasma Block Explorer....");

            var curPath = Directory.GetCurrentDirectory();
            Console.WriteLine("Current path: " + curPath);

            IServiceCollection serviceCollection = new ServiceCollection();
            _app = new AppServices(serviceCollection);

            var context = AppServices.GetService<ExplorerDbContext>();

            context.Database.Migrate();

            Console.WriteLine("Initializing db...");
            await ExplorerInicializer.Initialize(context);
            Console.WriteLine("Finished initializing db...");
            Console.WriteLine();
            Console.WriteLine("START sync");
            ExplorerSync.StartSync();

            var server = HostBuilder.CreateServer(args);

            Console.WriteLine("Setup UI stuff db...");
            var viewsRenderer = new ViewsRenderer(server, "views");
            viewsRenderer.Init();
            viewsRenderer.SetupHandlers();
            
            Console.WriteLine("READY");
            server.Run();
        }
    }
}
