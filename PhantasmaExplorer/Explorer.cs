using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Phantasma.API;
using Phantasma.Blockchain;
using Phantasma.Blockchain.Plugins;
using Phantasma.Blockchain.Tokens;
using Phantasma.Cryptography;
using Phantasma.Explorer.Infrastructure.Data;
using Phantasma.Explorer.Persistance;
using Phantasma.Explorer.Site;
using Phantasma.RpcClient;
using Phantasma.RpcClient.Interfaces;
using Phantasma.Tests;

namespace Phantasma.Explorer
{
    public class Explorer
    {
        //todo remove from here
        private static MockRepository _repo;

        public static IServiceProvider AppServices => _app.Services;
        private static Application _app;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Initializing Phantasma Block Explorer....");
            InitMockData();

            var curPath = Directory.GetCurrentDirectory();
            Console.WriteLine("Current path: " + curPath);

            IServiceCollection serviceCollection = new ServiceCollection();
            _app = new Application(serviceCollection);

            var server = HostBuilder.CreateServer(args);

            var mockRepo = new MockRepository();
            _repo = mockRepo;

            var viewsRenderer = new ViewsRenderer(server, "views");
            viewsRenderer.SetupControllers(mockRepo);
            viewsRenderer.Init();
            viewsRenderer.SetupHandlers();

            await mockRepo.InitRepo();
            server.Run();
        }

        private static Nexus InitMockData()
        {
            var ownerKey = KeyPair.FromWIF("L2G1vuxtVRPvC6uZ1ZL8i7Dbqxk9VPXZMGvZu9C3LXpxKK51x41N");
            var simulator = new ChainSimulator(ownerKey, 12346);

            // setup plugins required for explorer
            simulator.Nexus.AddPlugin(new ChainAddressesPlugin());
            simulator.Nexus.AddPlugin(new AddressTransactionsPlugin());
            simulator.Nexus.AddPlugin(new TokenTransactionsPlugin());

            // generate blocks with mock transactions
            for (int i = 1; i <= 50; i++)
            {
                simulator.GenerateRandomBlock();
            }

            var airdropFile = "nacho_addresses.txt";
            if (File.Exists(airdropFile))
            {
                Console.WriteLine("Loading airdrops from " + airdropFile);

                var lines = File.ReadAllLines(airdropFile);

                var addresses = new List<Address>();
                for (int i = 0; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i]))
                    {
                        continue;
                    }

                    addresses.Add(Address.FromText(lines[i]));

                    if (addresses.Count >= 10)
                    {
                        FlushAirdrop(ownerKey, simulator, addresses);
                    }
                }

                if (addresses.Count > 0)
                {
                    FlushAirdrop(ownerKey, simulator, addresses);
                }
            }

            var mempool = new Mempool(ownerKey, simulator.Nexus);
            mempool.Start();

            //todo rpc move
            var rpc = new RPCServer(new NexusAPI(simulator.Nexus, mempool), "rpc", 7077);
            rpc.Start();

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                while (mempool.IsRunning)
                {
                    Thread.Sleep(1000 * 60);
                    simulator.CurrentTime = DateTime.Now;
                    simulator.GenerateRandomBlock(mempool);
                }
            }).Start();

            new Thread(async () =>
            {
                Thread.CurrentThread.IsBackground = true;
                while (true)
                {
                    Thread.Sleep(1000);
                    await _repo.SyncronizeNewBlocks();
                }
            }).Start();

            return simulator.Nexus;
        }

        private static void FlushAirdrop(KeyPair keyPair, ChainSimulator simulator, List<Address> addresses)
        {
            simulator.BeginBlock();

            foreach (var address in addresses)
            {
                simulator.GenerateTransfer(keyPair, address, simulator.Nexus.RootChain, simulator.Nexus.NativeToken, TokenUtils.ToBigInteger(250, Nexus.NativeTokenDecimals));
            }

            simulator.EndBlock();

            addresses.Clear();
        }

        public static string MockLogoUrl = "https://s2.coinmarketcap.com/static/img/coins/32x32/2827.png";
    }

    public class Application
    {
        public IServiceProvider Services { get; set; }

        public Application(IServiceCollection serviceCollection)
        {
            ConfigureServices(serviceCollection);
            Services = serviceCollection.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddDbContext<ExplorerDbContext>();

            serviceCollection.AddScoped<IPhantasmaRpcService>(provider => new PhantasmaRpcService(new RpcClient.Client.RpcClient(new Uri("http://localhost:7077/rpc"), httpClientHandler: new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            })));
        }
    }
}
