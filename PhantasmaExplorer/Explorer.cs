using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Phantasma.Core.Utils;
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
            var settings = new Arguments(args);
            string rpcUrl = settings.GetString("rpc", "http://localhost:7077/rpc");
            AppSettings.RpcServerUrl = rpcUrl;

            PrintAscii();
            Console.WriteLine("\n");
            Console.WriteLine("Initializing Phantasma Block Explorer on port 7072....");

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
                Console.WriteLine("4 - Change RPC server");
                Console.WriteLine("5 - Ensure no missing blocks");
                Console.WriteLine("6 - Update all address balances");
                Console.WriteLine("7 - Delete DB");
                Console.WriteLine("8 - Exit");
                var option = Console.ReadKey().KeyChar;

                switch (option)
                {
                    case '1':
                        ExplorerSync.ContinueSync = false;
                        Console.Clear();
                        Console.WriteLine("Initializing db...");
                        await InitDb();
                        break;

                    case '2':
                        Console.Clear();
                        ExplorerSync.ContinueSync = true;
                        Console.WriteLine("Starting sync process");
                        await Sync();
                        break;

                    case '3':
                        ExplorerSync.ContinueSync = false;
                        break;

                    case '4':
                        Console.Clear();
                        exit = true;
                        await ChangeRpcServer();
                        break;

                    case '5':
                        ExplorerSync.ContinueSync = false;
                        Console.Clear();
                        EnsureNoMissingBlocks();
                        break;

                    case '6':
                        ExplorerSync.ContinueSync = false;
                        Console.Clear();
                        ExplorerSync.UpdateAllAddressBalances();
                        break;

                    case '7':
                        ExplorerSync.ContinueSync = false;
                        Console.Clear();
                        await DropDb();
                        break;
                    case '8':
                        exit = true;
                        break;
                }
            }
        }

        private static void PrintAscii()
        {
            Console.Write(@"dyo/ommmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmo/ohm
./yyo---::::::::::::::::::::::::::::::::::---oyyo/
.-:::++++++++++++++++++++++++++++++++++++++++::-..
-:/ooooooooooooooooooooooooooooooooooooooooooo+/:-
/+oooooooooooooooooooooooooooooooooooooooooooooo+/
+oooooooooooooooooooooooooooooooooooooooooooooooo+
ooooooooooooo+///+oooooooooooooo+///+ooooooooooooo
ooooooooooo:-.---../+oooooooo+/-.---`-:ooooooooooo
ooooooooo+/`-/ooo/-./+oooooo+/`:/ooo/-./+ooooooooo
ooooooooo/:.:oooso+.:+oooooo+:`/+ooss/-:/ooooooooo
ooooooooo++`-:+oo/:./oooooooo/.-:+oo/-.++ooooooooo
ooooooooooo+:-----:+oooooooooo+:-----:+ooooooooooo
oooooooooooooo+++oooooooooooooooo+++oooooooooooooo
oooooooooooooooooooooooooooooooooooooooooooooooooo
oooooooooooooooooooooooooooooooooooooooooooooooooo
oooooooooooooooooooooooooooooooooooooooooooooooooo
oooooooooooooooooooooooooooooooooooooooooooooooooo
oooooooooooooooooooooooooooooooooooooooooooooooooo
oooooooooooooooooooooooooooooooooooooooooooooooooo
oooooooooooooooooooooooooooooooooooooooooooooooooo
ooooo+ooooo+ooooooooooooooo+oooooooooo+ooooo+ooooo
o+++osooosoo++oooooooosooooooooooooo++oossooso+++o
yyyyhmddMmdyso+++ooosydmNNNmysoo++++osymmNddmhyyyy
dhhhddhdMmdyhdo++osdhhdNMNmdhhds+++odhymNMdhddhhhd
NNmmNMMMMMMmdhmmmmmymNMMMMMMNmymmmmmoymMMMMMMNmmNN");
        }

        private static void EnsureNoMissingBlocks()
        {
            var context = AppServices.GetService<ExplorerDbContext>();

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

        private static async Task DropDb()
        {
            var context = AppServices.GetService<ExplorerDbContext>();

            if (await context.Database.EnsureDeletedAsync())
            {
                Console.WriteLine("Database deleted with success");
            }
            else
            {
                Console.WriteLine("Error while deleting database");
            }
        }

        private static async Task InitDb()
        {
            var context = AppServices.GetService<ExplorerDbContext>();
            await context.Database.EnsureCreatedAsync();


            //context.Database.Migrate(); todo investigate

            if (!await ExplorerInicializer.Initialize(context))
            {
                Console.WriteLine("DB is already initialized");
            }
            else
            {
                Console.WriteLine("Finished initializing db...");
            }
        }

        private static async Task Sync()
        {
            var context = AppServices.GetService<ExplorerDbContext>();
            await context.Database.EnsureCreatedAsync();
            ExplorerSync.StartSync(context);
        }

        private static async Task ChangeRpcServer()
        {
            Console.WriteLine($"Current Rpc server URL: {AppSettings.RpcServerUrl}\n");

            Console.WriteLine("Type the new URL:");
            var url = Console.ReadLine();
            if (Uri.TryCreate(url, UriKind.Absolute, out var uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
            {
                AppSettings.RpcServerUrl = url;
                Console.WriteLine($"Rpc client URL changed with success to {uriResult.AbsoluteUri}.");
            }
            else
            {
                Console.WriteLine("Provided URL is invalid.");
            }

            await StartMenu();
        }
    }
}
