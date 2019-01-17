using System;
using System.IO;
using System.Threading;
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


            var server = HostBuilder.CreateServer(args);

            Console.WriteLine("Setup UI stuff db...");
            var viewsRenderer = new ViewsRenderer(server, "views");
            viewsRenderer.SetupControllers();
            viewsRenderer.Init();
            viewsRenderer.SetupHandlers();
            server.Run();
            new Thread(async () =>
            {
                Thread.CurrentThread.IsBackground = true;
                try
                {
                    await ExplorerSync.StartSync();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    await ExplorerSync.StartSync(); //todo this does not work
                }
            }).Start();


            Console.WriteLine("READY");
        }
        //L2G1vuxtVRPvC6uZ1ZL8i7Dbqxk9VPXZMGvZu9C3LXpxKK51x41N

        public static string MockLogoUrl = "https://s2.coinmarketcap.com/static/img/coins/32x32/2827.png";
    }
}
