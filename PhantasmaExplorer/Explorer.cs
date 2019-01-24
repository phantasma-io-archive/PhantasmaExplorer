using System;
using System.IO;
using System.Linq;
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

            Console.WriteLine("Setting up services");
            IServiceCollection serviceCollection = new ServiceCollection();
            _app = new AppServices(serviceCollection);


            Console.WriteLine("Setting up server and UI");
            var server = HostBuilder.CreateServer(args);
            var viewsRenderer = new ViewsRenderer(server, "views");
            viewsRenderer.Init();
            viewsRenderer.SetupHandlers();

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                server.Run();
            }).Start();

            await StartMenu();
        }

        private static async Task StartMenu()
        {
            bool exit = false;
            var context = AppServices.GetService<ExplorerDbContext>();
            while (!exit)
            {
                Console.WriteLine();
                Console.WriteLine("MENU");
                Console.WriteLine("1 - Init DB");
                Console.WriteLine("2 - Sync DB");
                Console.WriteLine("3 - Stop sync");
                Console.WriteLine("4 - Ensure no missing blocks");
                Console.WriteLine("5 - Delete DB");
                Console.WriteLine("6 - Exit");
                var option = Console.ReadKey().KeyChar;

                switch (option)
                {
                    case '1':
                        ExplorerSync.ContinueSync = false;
                        Console.Clear();
                        Thread.Sleep(2000);
                        Console.WriteLine("Initializing db...");
                        await InitDb(context);
                        break;

                    case '2':
                        Console.Clear();
                        Thread.Sleep(2000);
                        ExplorerSync.ContinueSync = true;
                        Console.WriteLine("Starting sync process");
                        ExplorerSync.StartSync();
                        break;

                    case '3':
                        ExplorerSync.ContinueSync = false;
                        break;

                    case '4':
                        ExplorerSync.ContinueSync = false;
                        Console.Clear();
                        EnsureNoMissingBlocks(context);

                        break;
                    case '5':
                        ExplorerSync.ContinueSync = false;
                        Console.Clear();
                        await DropDb(context);
                        break;

                    case '6':
                        exit = true;
                        break;
                }
            }
        }

        private static void EnsureNoMissingBlocks(ExplorerDbContext context)
        {
            foreach (var chain in context.Chains.Include(p => p.Blocks))
            {
                Console.WriteLine($"Checking {chain.Name} chain blocks...");
                int heightCounter = 0;
                foreach (var chainBlock in chain.Blocks.OrderBy(p => p.Height))
                {
                    heightCounter++;
                    if (heightCounter != chainBlock.Height)
                    {
                        Console.WriteLine($"Chain {chain.Name} has missing blocks!");
                        Console.WriteLine("Please delete DB and start over the initialization process.");
                        return;
                    }
                }
            }
            Console.WriteLine("No blocks missing. Success!");
        }

        private static async Task DropDb(ExplorerDbContext context)
        {
            if (await context.Database.EnsureDeletedAsync())
            {
                Console.WriteLine("Database deleted with success");
            }
            else
            {
                Console.WriteLine("Error while deleting database");
            }
        }

        private static async Task InitDb(ExplorerDbContext context)
        {
            context.Database.Migrate();
            if (!await ExplorerInicializer.Initialize(context))
            {
                Console.WriteLine("DB is already initialized");
            }
            else
            {
                Console.WriteLine("Finished initializing db...");
            }
        }
    }
}
