using System;
using System.IO;
using Phantasma.Blockchain;
using Phantasma.Blockchain.Plugins;
using Phantasma.Cryptography;
using Phantasma.Explorer.Infrastructure.Data;
using Phantasma.Explorer.Site;
using Phantasma.Tests;

namespace Phantasma.Explorer
{
    public class Explorer
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Initializing Phantasma Block Explorer....");
            var nexus = InitMockData();

            var curPath = Directory.GetCurrentDirectory();
            Console.WriteLine("Current path: " + curPath);

            var site = HostBuilder.CreateSite(args, "public");
            var viewsRenderer = new ViewsRenderer(site, "views");

            var mockRepo = new MockRepository { NexusChain = nexus };

            viewsRenderer.SetupControllers(mockRepo);
            viewsRenderer.Init();
            viewsRenderer.SetupHandlers();
            site.server.Run(site);
        }

        private static Nexus InitMockData()
        {
            var ownerKey = KeyPair.FromWIF("L2G1vuxtVRPvC6uZ1ZL8i7Dbqxk9VPXZMGvZu9C3LXpxKK51x41N");
            var simulator = new ChainSimulator(ownerKey, 12345);

            // setup plugins required for explorer
            simulator.Nexus.AddPlugin(new ChainAddressesPlugin(simulator.Nexus));
            simulator.Nexus.AddPlugin(new AddressTransactionsPlugin(simulator.Nexus));
            simulator.Nexus.AddPlugin(new TokenTransactionsPlugin(simulator.Nexus));

            // generate blocks with mock transactions
            for (int i=1; i<=1000; i++)
            {
                simulator.GenerateRandomBlock();
            }

            return simulator.Nexus;
        }
    }
}
