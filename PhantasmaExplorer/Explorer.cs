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
            while (!exit)
            {
                Console.WriteLine();
                Console.WriteLine("MENU");
                Console.WriteLine("1 - Init DB");
                Console.WriteLine("2 - Sync DB");
                Console.WriteLine("3 - Stop sync");
                Console.WriteLine("4 - Delete DB");
                Console.WriteLine("5 - Exit");
                var option = Console.ReadKey().KeyChar;

                switch (option)
                {
                    case '1':
                        await InitDb();
                        break;

                    case '2':
                        Console.Clear();
                        Thread.Sleep(2000);
                        ExplorerSync.ContinueSync = true;
                        ExplorerSync.StartSync();
                        break;

                    case '3':
                        ExplorerSync.ContinueSync = false;
                        break;

                    case '4':
                        DropDb();
                        break;
                    case '5':
                        exit = true;
                        break;
                }
            }

        }

        private static void DropDb()
        {
            throw new NotImplementedException();
        }

        private static async Task InitDb()
        {
            var context = AppServices.GetService<ExplorerDbContext>();

            context.Database.Migrate();

            Console.WriteLine("Initializing db...");
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
