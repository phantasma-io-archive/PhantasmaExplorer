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
using Phantasma.VM.Contracts;

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
        public Block block;
        public DateTime date;
        public string chainName;
        public string chainAddress;
        public string fromName;
        public string fromAddress;

        public static TransactionContext FromTransaction(Nexus nexus, Block block, Transaction tx)
        {
            return new TransactionContext()
            {
                block = block,
                chainAddress = block.Chain.Address.Text,
                chainName = block.Chain.Name,
                date = block.Timestamp,
                hash = tx.Hash.ToString(),
                fromAddress = "????",
                fromName = "Anonymous",
            };
        }
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

    public struct BlockContext
    {
        public int height;
        public DateTime timestamp;
        public int transactions;
        public string hash;
        public string parentHash;
        public string miningAddress;
        public string chainName;
        public string chainAddress;

        public static BlockContext FromBlock(Block block)
        {
            return new BlockContext
            {
                height = (int) block.Height,
                timestamp = block.Timestamp,
                transactions = block.Transactions.Count(),
                hash = block.Hash.ToString(),
                parentHash = block.PreviousHash?.ToString(),
                miningAddress = block.MinerAddress.Text,
                chainName = block.Chain.Name.ToTitleCase(),
                chainAddress = block.Chain.Address.Text
            };
        }
    }

    public struct AddressContext
    {
        public string address;//todo change var name
        public string name;
        public decimal balance;
        public decimal value;
        public List<TransactionContext> transactions;

        public static AddressContext FromAddress(Nexus nexus, Address address)
        {
            var balance = TokenUtils.ToDecimal(nexus.RootChain.GetTokenBalance(nexus.NativeToken, address));

            return new AddressContext()
            {
                address = address.Text,
                name = "Anonymous",
                balance = balance,
                value = balance * Explorer.soulRate,
                transactions = new List<TransactionContext>(),
            };
        }
    }

    public struct ChainContext
    {
        public string address;
        public string name;
        public int transactions;
        public int height;
    }

    public class Explorer
    {
        public static decimal soulRate { get; private set; }

        private static Dictionary<string, object> CreateContext()
        {
            var context = new Dictionary<string, object>();

            // TODO this should not be created at each request...
            var menus = new List<MenuContext>();
            menus.Add(new MenuContext() { text = "Transactions", url = "/transactions", active = true });
            menus.Add(new MenuContext() { text = "Chains", url = "/chains", active = false });
            menus.Add(new MenuContext() { text = "Blocks", url = "/blocks", active = false });
            menus.Add(new MenuContext() { text = "Tokens", url = "/tokens", active = false });
            menus.Add(new MenuContext() { text = "Addresses", url = "/addresses", active = false });

            context["menu"] = menus;

            return context;
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Initializing Phantasma Block Explorer....");

            var ownerKey = KeyPair.FromWIF("L2G1vuxtVRPvC6uZ1ZL8i7Dbqxk9VPXZMGvZu9C3LXpxKK51x41N");
            var nexus = new Nexus(ownerKey);

            var targetAddress = Address.FromText("PGasVpbFYdu7qERihCsR22nTDQp1JwVAjfuJ38T8NtrCB");
            {
                var transactions = new List<Transaction>();
                var script = ScriptUtils.CallContractScript(nexus.RootChain, "TransferTokens", ownerKey.Address, targetAddress, Nexus.NativeTokenSymbol, TokenUtils.ToBigInteger(5));
                var tx = new Transaction(script, 0, 0);
                tx.Sign(ownerKey);
                transactions.Add(tx);

                var block = new Block(nexus.RootChain, ownerKey.Address, Timestamp.Now, transactions, nexus.RootChain.lastBlock);
                if (!nexus.RootChain.AddBlock(block))
                {
                    throw new Exception("test block failed");
                }
            }

            // TODO this should be updated every 5 minutes or so
            soulRate = CoinUtils.GetCoinRate(2827);

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

                var txList = new List<TransactionContext>();
                foreach (var chain in nexus.Chains)
                {
                    foreach (var block in chain.Blocks.TakeLast(20))
                    {
                        foreach (var tx in block.Transactions)
                        {
                            txList.Add(TransactionContext.FromTransaction(nexus, block, (Transaction)tx));
                        }
                    }
                }

                context["transactions"] = txList;
                return templateEngine.Render(site, context, new string[] { "layout", "transactions" });
            });

            site.Get("/addresses", (request) =>
            {
                var addressList = new List<AddressContext>();

                addressList.Add(AddressContext.FromAddress(nexus, ownerKey.Address));
                addressList.Add(AddressContext.FromAddress(nexus, targetAddress));

                var context = CreateContext();
                context["addresses"] = addressList;

                return templateEngine.Render(site, context, new string[] { "layout", "addresses" });
            });

            site.Get("/chains", (request) =>
            {
                var context = CreateContext();

                var chainList = new List<ChainContext>();
                foreach (var chain in nexus.Chains)
                {
                    chainList.Add(new ChainContext()
                    {
                        address = chain.Address.Text,
                        name = chain.Name.ToTitleCase(),
                        transactions = chain.TransactionCount,
                        height = chain.Blocks.Count()
                    });
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
                var address = Address.FromText(addressText);

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

                var addressDto = AddressContext.FromAddress(nexus, address);

                var context = CreateContext();

                context["address"] = addressDto;
                context["transactions"] = addressDto.transactions;

                return templateEngine.Render(site, context, new string[] { "layout", "address" });
            });

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

            site.Get("/tx/{input}", (request) =>
            {
                var addressText = request.GetVariable("input");
                var hash = Hash.Parse(addressText);

                var tx = nexus.RootChain.FindTransaction(hash);
                var block = nexus.RootChain.FindTransactionBlock(tx);

                var context = CreateContext();
                context["transaction"] = TransactionContext.FromTransaction(nexus, block, tx);
                return templateEngine.Render(site, context, new string[] { "layout", "transaction" });
            });

            site.Get("/block/{input}", (request) => //input can be height or hash
            {
                var input = request.GetVariable("input");
                Block block;
                if(int.TryParse(input, out var height))
                {
                    block = nexus.RootChain.FindBlock(height);
                }
                else
                {
                    // todo validate hash
                    block = nexus.RootChain.FindBlock(Hash.Parse(input));
                }
               
                var context = CreateContext();
                if (block != null)
                {
                    context["block"] = BlockContext.FromBlock(block);
                }

                return templateEngine.Render(site, context, new string[] { "layout", "block" });
            });

            site.Get("/blocks", (request) => //input can be height or hash
            {
                List<Block> tempList = new List<Block>();
                 
                var blocksTemp = new List<BlockContext>();

                foreach (var chain in nexus.Chains)
                {
                    if (chain.Blocks.Any())
                    {
                        tempList.AddRange(chain.Blocks.TakeLast(20));
                    }
                }

                tempList = tempList.OrderBy(block=>block.Timestamp.Value).ToList();
                foreach (var block in tempList)
                {
                    blocksTemp.Add(BlockContext.FromBlock(block));
                }

                var context = CreateContext();
                context["blocks"] = blocksTemp;

                return templateEngine.Render(site, context, new string[] { "layout", "blocks" });
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
            var dt = (DateTime)stamp;
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
