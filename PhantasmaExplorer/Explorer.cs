using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LunarLabs.WebServer.Core;
using LunarLabs.WebServer.HTTP;
using LunarLabs.WebServer.Templates;
using Phantasma.Core.Utils;
using Phantasma.Blockchain;
using Phantasma.Core.Types;
using Phantasma.Cryptography;
using Phantasma.Explorer;
using Phantasma.Numerics;
using Phantasma.VM.Utils;
using Phantasma.Core.Types;

namespace PhantasmaExplorer
{
    public struct MenuContext
    {
        public string text;
        public string url;
        public bool active;
    }

    public struct TransactionContext
    {
        public string hash;
        public DateTime date;
        public string chainName;
        public string chainAddress;
    }

    public struct TokenContext
    {
        public string symbol;
        public string name;
        public string logoUrl;
        public string description;
        public string contractHash;
        public int decimals;
        public decimal maxSupply;
        public decimal currentSupply;
    }

    public struct AddressContext
    {
        public string address;//todo change var name
        public decimal soulBalance;
        public decimal soulValue;
        public int numberOfTransactions;
        public List<TransactionContext> transactions;
    }

    public struct ChainContext
    {
        public string address;
        public string name;
        public int transactions;
    }

    public class Explorer
    {

        private static Dictionary<string, object> CreateContext()
        {
            var context = new Dictionary<string, object>();

            // TODO this should not be created at each request...
            var menus = new List<MenuContext>();
            menus.Add(new MenuContext() { text = "Transactions", url = "/transactions", active = true });
            menus.Add(new MenuContext() { text = "Chains", url = "/chains", active = false });
            menus.Add(new MenuContext() { text = "Tokens", url = "/tokens", active = false });
            menus.Add(new MenuContext() { text = "Addresses", url = "/addresses", active = false });

            context["menu"] = menus;

            return context;
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Initializing Phantasma Block Explorer....");

            var ownerKey = KeyPair.Generate();
            var nexus = new Nexus(ownerKey);

            var targetAddress = Address.FromText("PGasVpbFYdu7qERihCsR22nTDQp1JwVAjfuJ38T8NtrCB");
            var transactions = new List<Transaction>();
            var script = ScriptUtils.CallContractScript(nexus.RootChain, "TransferTokens", ownerKey.Address, targetAddress, Nexus.NativeTokenSymbol, new BigInteger(500000));
            var tx = new Transaction(script, 0, 0);
            tx.Sign(ownerKey);
            transactions.Add(tx);

            var block = new Block(nexus.RootChain, ownerKey.Address, Timestamp.Now, transactions, nexus.RootChain.lastBlock);
            if (!nexus.RootChain.AddBlock(block))
            {
                throw new Exception("test block failed");
            }

            var curPath = Directory.GetCurrentDirectory();
            Console.WriteLine("Current path: " + curPath);

            // initialize a logger
            var log = new ConsoleLogger();

            // either parse the settings from the program args or initialize them manually
            var settings = ServerSettings.Parse(args);

            var server = new HTTPServer(log, settings);

            // instantiate a new site, the second argument is the relative file path where the public site contents will be found
            var site = new Site(server, "public");

            var templateEngine = new TemplateEngine(site, "views");

            site.Get("/", (request) =>
            {
                return HTTPResponse.Redirect("/transactions");
            });

            site.Get("/transactions", (request) =>
            {
                var context = CreateContext();

                // placeholders
                var txList = new List<TransactionContext>();
                txList.Add(new TransactionContext() { hash = "0xFFABABCACAACAFF", date = DateTime.Now - TimeSpan.FromMinutes(12), chainName = "Main", chainAddress = "fixme" });
                txList.Add(new TransactionContext() { hash = "0xABABCACAACAFFAA", date = DateTime.Now - TimeSpan.FromMinutes(5), chainName = "Main", chainAddress = "fixme" });
                txList.Add(new TransactionContext() { hash = "0xFFCCAACACCACACA", date = DateTime.Now, chainName = "Main", chainAddress = "fixme" });

                context["transactions"] = txList;
                return templateEngine.Render(site, context, new string[] { "layout", "transactions" });
            });

            site.Get("/addresses", (request) =>
            {
                //foreach (var nexusChain in nexus.Chains)
                //{
                //    nexusChain.Address;
                //}
                var context = CreateContext();
                return templateEngine.Render(site, context, new string[] { "layout", "addresses" });
            });

            site.Get("/chains", (request) =>
            {
                var context = CreateContext();

                var chainList = new List<ChainContext>();
                foreach (var chain in nexus.Chains)
                {
                    chainList.Add(new ChainContext() { address = chain.Address.Text, name = chain.Name.ToTitleCase(), transactions = 0 });
                }

                context["chains"] = chainList;

                return templateEngine.Render(site, context, new string[] { "layout", "chains" });
            });

            site.Get("/tokens", (request) =>
            {
                var context = CreateContext();
                var nexusTokens = nexus.Tokens.ToList();
                //Placeholders todo move this
                var tokensList = new List<TokenContext>();
                foreach (var token in nexusTokens)
                {
                    tokensList.Add(new TokenContext
                    {
                        name = token.Name,
                        symbol = token.Symbol,
                        decimals = (int)token.GetDecimals(),
                        description = "Soul is the native asset of Phantasma blockchain",
                        logoUrl = "https://s2.coinmarketcap.com/static/img/coins/32x32/2827.png",
                        contractHash = "hash here?",
                        currentSupply = (decimal)token.CurrentSupply,
                        maxSupply = (decimal)token.MaxSupply,
                    });
                }
                context["tokens"] = tokensList;
                return templateEngine.Render(site, context, new string[] { "layout", "tokens" });
            });

            // TODO address.html view 
            site.Get("/address/{input}", (request) =>
            {
                var addressText = request.GetVariable("input");
                var address = Phantasma.Cryptography.Address.FromText(addressText);

                // todo move this
                var soulRate = CoinUtils.GetCoinRate(2827);

                var mockTransactionList = new List<TransactionContext>();
                foreach (var nexusChain in nexus.Chains)
                {
                    mockTransactionList.Add(new TransactionContext()
                    {
                        chainAddress = nexusChain.Address.Text,
                        date = DateTime.Now,
                        hash = "test",
                        chainName = nexusChain.Name,
                    });
                }

                var addressDto = new AddressContext
                {
                    address = address.Text,
                    numberOfTransactions = 10,
                    soulBalance = 13.32m,
                    transactions = mockTransactionList
                };
                addressDto.soulValue = addressDto.soulBalance * soulRate;

                var context = CreateContext();

                context["address"] = addressDto;
                context["transactions"] = addressDto.transactions;

                return templateEngine.Render(site, context, new string[] { "layout", "address" });
            });

            // TODO chain.html view 
            site.Get("/chain/{input}", (request) => //todo this could be the name of the chain rather then the address?
            {
                var addressText = request.GetVariable("input");
                var chainAddress = Phantasma.Cryptography.Address.FromText(addressText);
                var chain = nexus.Chains.SingleOrDefault(c => c.Address == chainAddress);

                var context = CreateContext();
                if (chain != null)
                {
                    var blocks = chain.Blocks.ToList().TakeLast(20);
                    context["blocks"] = blocks;
                }
                context["chain"] = chain;
               
                return templateEngine.Render(site, context, new string[] { "layout", "chain" });
            });

            // TODO transaction.html view 
            site.Get("/tx/{input}", (request) =>
            {
                //var addressText = request.GetVariable("input");
                //var address = Phantasma.Cryptography.Address.FromText(addressText);

                //placeholder
                var txc = new TransactionContext
                {
                    chainAddress = "test",
                    chainName = "test",
                    date = DateTime.Now,
                    hash = "test"
                };

                var context = CreateContext();
                context["transaction"] = txc;
                return templateEngine.Render(site, context, new string[] { "layout", "transaction" });
            });

            server.Run();
        }




        //todo move this
        private string RelativeTime(Timestamp stamp)
        {
            const int SECOND = 1;
            const int MINUTE = 60 * SECOND;
            const int HOUR = 60 * MINUTE;
            const int DAY = 24 * HOUR;
            const int MONTH = 30 * DAY;
            var dt = (DateTime) stamp;
            var ts = new TimeSpan(DateTime.UtcNow.Ticks - dt.Ticks);
            double delta = Math.Abs(ts.TotalSeconds);

            if (delta < 1 * MINUTE)
                return ts.Seconds == 1 ? "one second ago" : ts.Seconds + " seconds ago";

            if (delta < 2 * MINUTE)
                return "a minute ago";

            if (delta < 45 * MINUTE)
                return ts.Minutes + " minutes ago";

            if (delta < 90 * MINUTE)
                return "an hour ago";

            if (delta < 24 * HOUR)
                return ts.Hours + " hours ago";

            if (delta < 48 * HOUR)
                return "yesterday";

            if (delta < 30 * DAY)
                return ts.Days + " days ago";

            if (delta < 12 * MONTH)
            {
                int months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
                return months <= 1 ? "one month ago" : months + " months ago";
            }
            else
            {
                int years = Convert.ToInt32(Math.Floor((double)ts.Days / 365));
                return years <= 1 ? "one year ago" : years + " years ago";
            }
        }
    }
}
