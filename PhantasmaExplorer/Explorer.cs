using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LunarLabs.WebServer.Core;
using LunarLabs.WebServer.HTTP;
using LunarLabs.WebServer.Templates;
using Phantasma.Core.Utils;
using Phantasma.Blockchain;
using Phantasma.Cryptography;
using Phantasma.Explorer;
using Phantasma.Numerics;

namespace PhantasmaExplorer
{
    public struct Menu
    {
        public string text;
        public string url;
        public bool active;
    }

    public struct Transaction
    {
        public string hash;
        public DateTime date;
        public string chainName;
        public string chainAddress;
    }

    public struct Token
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

    public struct Address
    {
        public string address;//todo change var name
        public decimal soulBalance;
        public decimal soulValue;
        public int numberOfTransactions;
        public List<Transaction> transactions;
    }

    public struct Chain
    {
        public string address;
        public string name;
        public int transactions;
    }

    class Explorer
    {

        private static Dictionary<string, object> CreateContext()
        {
            var context = new Dictionary<string, object>();

            // TODO this should not be created at each request...
            var menus = new List<Menu>();
            menus.Add(new Menu() { text = "Transactions", url = "/transactions", active = true });
            menus.Add(new Menu() { text = "Chains", url = "/chains", active = false });
            menus.Add(new Menu() { text = "Tokens", url = "/tokens", active = false });
            menus.Add(new Menu() { text = "Addresses", url = "/addresses", active = false });

            context["menu"] = menus;

            return context;
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Initializing Phantasma Block Explorer....");

            var ownerKey = KeyPair.Generate();
            var nexus = new Nexus(ownerKey);

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
                var txList = new List<Transaction>();
                txList.Add(new Transaction() { hash = "0xFFABABCACAACAFF", date = DateTime.Now - TimeSpan.FromMinutes(12), chainName = "Main", chainAddress = "fixme" });
                txList.Add(new Transaction() { hash = "0xABABCACAACAFFAA", date = DateTime.Now - TimeSpan.FromMinutes(5), chainName = "Main", chainAddress = "fixme" });
                txList.Add(new Transaction() { hash = "0xFFCCAACACCACACA", date = DateTime.Now, chainName = "Main", chainAddress = "fixme" });

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

                var chainList = new List<Chain>();
                foreach (var chain in nexus.Chains)
                {
                    chainList.Add(new Chain() { address = chain.Address.Text, name = chain.Name.ToTitleCase(), transactions = 0 });
                }

                context["chains"] = chainList;

                return templateEngine.Render(site, context, new string[] { "layout", "chains" });
            });

            site.Get("/tokens", (request) =>
            {
                var context = CreateContext();
                var nexusTokens = nexus.Tokens.ToList();
                //Placeholders todo move this
                var tokensList = new List<Token>();
                foreach (var token in nexusTokens)
                {
                    tokensList.Add(new Token
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

                var mockTransactionList = new List<Transaction>();
                foreach (var nexusChain in nexus.Chains)
                {
                    mockTransactionList.Add(new Transaction()
                    {
                        chainAddress = nexusChain.Address.Text,
                        date = DateTime.Now,
                        chainName = nexusChain.Name,
                        hash = "todo"
                    });
                }

                var addressDto = new Address
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

                var blocks = chain.Blocks.ToList().TakeLast(10);

                var context = CreateContext();
                context["chain"] = chain;
                context["blocks"] = chain.Blocks;
                return templateEngine.Render(site, context, new string[] { "layout", "chain" });
            });

            // TODO transaction.html view 
            site.Get("/tx/{input}", (request) =>
            {
                var addressText = request.GetVariable("input");
                var address = Phantasma.Cryptography.Address.FromText(addressText);

                var context = CreateContext();
                return templateEngine.Render(site, context, new string[] { "layout", "transaction" });
            });

            server.Run();
        }
    }
}
