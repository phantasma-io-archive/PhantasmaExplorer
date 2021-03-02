using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

using LunarLabs.WebServer.Core;
using LunarLabs.WebServer.HTTP;
using LunarLabs.WebServer.Templates;

using Phantasma.Core.Utils;
using Phantasma.Cryptography;
using Phantasma.Domain;
using Phantasma.Explorer;

namespace PhantasmaExplorer
{
    public struct MenuContext
    {
        public string Text;
        public string Url;
        public bool Active;
    }

    public struct SearchResultWithURL
    {
        public SearchResultKind Kind;
        public string Text;
        public string URL;
    }

    public struct HomeContext
    {
        public int TotalChains;
        public int TotalTransactions;
        public uint BlockHeight;
        public IEnumerable<BlockData> Blocks;
        public IEnumerable<TransactionData> Transactions;
        //public Dictionary<string, uint> Chart;
    }

    public class Explorer
    {
        public const string ExplorerVersion = "1.1.4";

        public static NexusData nexus { get; private set; }

        private static List<MenuContext> menus;

        private static Dictionary<string, object> CreateContext()
        {
            var context = new Dictionary<string, object>();

            context["menu"] = menus;
            context["explorerVersion"] = ExplorerVersion;

            return context;
        }

        static string Error(TemplateEngine templateEngine, string msg)
        {
            var context = CreateContext();
            context["error"] = msg;
            return templateEngine.Render(context, "layout", "error");
        }

        static string GetURLForSearch(SearchResultKind kind, string data)
        {
            switch (kind)
            {
                case SearchResultKind.Chain:
                    return $"/chain/{data}";

                case SearchResultKind.Address:
                    return $"/address/{data}";

                case SearchResultKind.Transaction:
                    return $"/tx/{data}";

                case SearchResultKind.Block:
                    return $"/block/{data}";

                case SearchResultKind.Organization:
                    return $"/dao/{data}";

                case SearchResultKind.Leaderboard:
                    return $"/leaderboard/{data}";

                case SearchResultKind.Token:
                    return $"/token/{data}";

                case SearchResultKind.Contract:
                    return $"/contract/{data}";

                case SearchResultKind.Sale:
                    return $"/sale/{data}";

                default:
                    return $"error";
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Initializing Phantasma Block Explorer....");

            menus = new List<MenuContext>();
            menus.Add(new MenuContext() { Text = "Blocks", Url = "/chain/main", Active = true });
            //menus.Add(new MenuContext() { Text = "Transactions", Url = "/transactions", Active = true });
            // menus.Add(new MenuContext() { Text = "Chains", Url = "/chains", Active = false });
            //menus.Add(new MenuContext() { Text = "Blocks", Url = "/blocks", Active = false });
            // menus.Add(new MenuContext() { Text = "Tokens", Url = "/tokens", Active = false });
            //menus.Add(new MenuContext() { Text = "Addresses", Url = "/addresses", Active = false });

            // TODO this should be updated every 5 minutes or so
            //soulRate = CoinUtils.GetCoinRate(2827);

            var defaultCachePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/Cache";

            var explorerArgs = new Arguments(args);
            var restURL = explorerArgs.GetString("phantasma.rest", "http://localhost:7078/api");
            var cachePath = explorerArgs.GetString("cache.path", defaultCachePath);
            nexus = new NexusData(restURL, cachePath);

            if (!string.IsNullOrEmpty(cachePath))
            {
                Console.WriteLine("Explorer cache path: " + cachePath);
                if (!Directory.Exists(cachePath))
                {
                    Directory.CreateDirectory(cachePath);
                }
            }
            else
            {
                Console.WriteLine("Explorer cache not set");
            }

            Console.WriteLine("Connecting explorer via REST: " + restURL);

            bool initialized = false;
            string nexusError = null;

            new Thread(() =>
            {
                if (!nexus.Update())
                {
                    nexusError = "Initialization failed!";
                }

                initialized = true;
                Console.WriteLine("Explorer is now ready");
            }).Start();

            var curPath = Directory.GetCurrentDirectory();
            Console.WriteLine("Current path: " + curPath);

            // initialize a logger
            // either parse the settings from the program args or initialize them manually
            var settings = ServerSettings.Parse(args);

            var server = new HTTPServer(settings, ConsoleLogger.Write);

            var templateEngine = new TemplateEngine(server, "views");
            templateEngine.Compiler.RegisterTag("value", (doc, val) => new ValueTag(doc, val));
            templateEngine.Compiler.RegisterTag("number", (doc, val) => new NumberTag(doc, val));
            templateEngine.Compiler.RegisterTag("hex", (doc, val) => new HexTag(doc, val));
            templateEngine.Compiler.RegisterTag("timeago", (doc, val) => new TimeAgoTag(doc, val));
            templateEngine.Compiler.RegisterTag("async", (doc, val) => new AsyncTag(doc, val));
            templateEngine.Compiler.RegisterTag("link-chain", (doc, val) => new LinkChainTag(doc, val));
            templateEngine.Compiler.RegisterTag("link-tx", (doc, val) => new LinkTransactionTag(doc, val));
            templateEngine.Compiler.RegisterTag("link-address", (doc, val) => new LinkAddressTag(nexus, doc, val));
            templateEngine.Compiler.RegisterTag("link-block", (doc, val) => new LinkBlockTag(doc, val));
            templateEngine.Compiler.RegisterTag("link-org", (doc, val) => new LinkOrganizationTag(doc, val));
            templateEngine.Compiler.RegisterTag("link-contract", (doc, val) => new LinkContractTag(doc, val));
            templateEngine.Compiler.RegisterTag("description", (doc, val) => new DescriptionTag(doc, val));
            templateEngine.Compiler.RegisterTag("externalLink", (doc, val) => new LinkExternalTag(doc, val));

            server.Get("/", (request) =>
            {
                return HTTPResponse.Redirect("/nexus");
            });

            server.Get("/progress", (request) =>
            {
                var progress = "{\"status\": \""+nexus.UpdateStatus+"\", \"percent\": "+nexus.UpdateProgress+"}";
                return HTTPResponse.FromString(progress, HTTPCode.OK, false, "application/json");
            });

            server.Get("/nexus", (request) =>
            {
                var context = CreateContext();

                if (!initialized)
                {
                    context["progress"] = nexus.UpdateProgress;
                    context["status"] = nexusError != null ? nexusError : nexus.UpdateStatus;
                    return templateEngine.Render(context, "layout", "init");
                }

                context["nexus"] = nexus;
                return templateEngine.Render(context, "layout", "nexus");
            });

            server.Get("/chain/{input}", (request) =>
            {
                if (!initialized)
                {
                    return HTTPResponse.Redirect("/nexus");
                }

                var chainName = request.GetVariable("input");

                var chain  = nexus.FindChainByName(chainName);
                if (chain != null)
                {
                    var context = CreateContext();
                    context["chain"] = chain;
                    return templateEngine.Render(context, "layout", "chain");
                }

                return Error(templateEngine, "Could not find chain: " + chainName);
            });

            server.Get("/chains", (request) =>
            {
                if (!initialized)
                {
                    return HTTPResponse.Redirect("/nexus");
                }

                var context = CreateContext();
                context["chains"] = nexus.Chains;

                return templateEngine.Render(context, "layout", "chains");
            });

            server.Get("/tokens", (request) =>
            {
                if (!initialized)
                {
                    return HTTPResponse.Redirect("/nexus");
                }

                var context = CreateContext();
                context["tokens"] = nexus.Tokens;

                return templateEngine.Render(context, "layout", "tokens");
            });

            server.Get("/block/{chain}/{hash}", (request) =>
            {
                if (!initialized)
                {
                    return HTTPResponse.Redirect("/nexus");
                }

                var chainName = request.GetVariable("chain");

                var chain = nexus.FindChainByName(chainName);
                if (chain != null)
                {
                    var hash = Hash.Parse(request.GetVariable("hash"));
                    var block = nexus.FindBlockByHash(chain, hash);
                    if (block != null)
                    {
                        var context = CreateContext();
                        context["block"] = block;
                        return templateEngine.Render(context, "layout", "block");
                    }
                    else
                    {
                        return Error(templateEngine, "Could not find block with hash: " + hash);
                    }
                }
                else
                {
                    return Error(templateEngine, "Could not find chain with name: " + chainName);
                }
            });

            server.Get("/height/{chain}/{index}", (request) =>
            {
                if (!initialized)
                {
                    return HTTPResponse.Redirect("/nexus");
                }

                var chainName = request.GetVariable("chain");

                var chain = nexus.FindChainByName(chainName);
                if (chain != null)
                {
                    var height = int.Parse(request.GetVariable("index"));
                    var block = height > 0 && height<= chain.BlockList.Count ? chain.BlockList[height-1] : null;

                    if (block != null)
                    {
                        var context = CreateContext();
                        context["block"] = block;
                        return templateEngine.Render(context, "layout", "block");

                    }
                    else
                    {
                        return Error(templateEngine, "Could not find block with height: " + height);
                    }
                }
                else
                {
                    return Error(templateEngine, "Could not find chain with name: " + chainName);
                }
            });

            server.Get("/tx/{input}", (request) =>
            {
                if (!initialized)
                {
                    return HTTPResponse.Redirect("/nexus");
                }

                var hash = Hash.Parse(request.GetVariable("input"));

                var tx = nexus.FindTransaction(nexus.RootChain, hash);
                if (tx != null)
                {
                    var context = CreateContext();
                    context["transaction"] = tx;

                    return templateEngine.Render(context, "layout", "transaction");
                }

                return Error(templateEngine, "Could not find transaction with hash: " + hash);
            });

            server.Get("/dao/{input}", (request) =>
            {
                if (!initialized)
                {
                    return HTTPResponse.Redirect("/nexus");
                }

                var id = request.GetVariable("input");

                var org = nexus.FindOrganization(id);
                if (org != null)
                {
                    var context = CreateContext();
                    context["dao"] = org;

                    return templateEngine.Render(context, "layout", "dao");
                }

                return Error(templateEngine, "Could not find organization with id: " + id);
            });

            server.Get("/contract/{input}", (request) =>
            {
                if (!initialized)
                {
                    return HTTPResponse.Redirect("/nexus");
                }

                var id = request.GetVariable("input");

                // TODO support chains other than root
                var contract = nexus.FindContract(DomainSettings.RootChainName, id);
                if (contract != null)
                {
                    var context = CreateContext();
                    context["contract"] = contract;

                    return templateEngine.Render(context, "layout", "contract");
                }

                return Error(templateEngine, "Could not find contract with id: " + id);
            });

            server.Get("/file/{input}", (request) =>
            {
                if (!initialized)
                {
                    return HTTPResponse.Redirect("/nexus");
                }

                var id = request.GetVariable("input");

                var file = nexus.FindFile(id);
                if (file != null)
                {
                    var context = CreateContext();
                    context["file"] = file;

                    return templateEngine.Render(context, "layout", "file");
                }

                return Error(templateEngine, "Could not find file with id: " + id);
            });

            server.Get("/sale/{input}", (request) =>
            {
                if (!initialized)
                {
                    return HTTPResponse.Redirect("/nexus");
                }

                var input = request.GetVariable("input");

                Hash saleHash;

                if (Hash.TryParse(input, out saleHash))
                {
                    var sale = nexus.FindSaleByHash(saleHash);
                    if (sale != null)
                    {
                        var context = CreateContext();
                        context["sale"] = sale;

                        return templateEngine.Render(context, "layout", "sale");
                    }
                }

                return Error(templateEngine, "Could not find sale with hash: " + input);
            });

            server.Get("/token/{input}", (request) =>
            {
                if (!initialized)
                {
                    return HTTPResponse.Redirect("/nexus");
                }

                var symbol = request.GetVariable("input");

                var token = nexus.FindTokenBySymbol(symbol);
                if (token != null)
                {
                    var context = CreateContext();
                    context["token"] = token;

                    return templateEngine.Render(context, "layout", "token");
                }

                return Error(templateEngine, "Could not find token with symbol: " + symbol);
            });

            server.Get("/address/{input}", (request) =>
            {
                if (!initialized)
                {
                    return HTTPResponse.Redirect("/nexus");
                }

                var input = request.GetVariable("input");

                if (!Address.IsValidAddress(input))
                {
                    return Error(templateEngine, "Could not find address: " + input);
                }

                var address = Address.FromText(input);

                var context = CreateContext();

                var account = nexus.FindAccount(address, true);
                if (account != null)
                {
                    context["account"] = account;
                }

                context["address"] = address;

                return templateEngine.Render(context, "layout", "account");
            });

            server.Get("/leaderboard/{input}", (request) =>
            {
                if (!initialized)
                {
                    return HTTPResponse.Redirect("/nexus");
                }

                var input = request.GetVariable("input");

                var leaderboard = nexus.FindLeaderboard(input);

                if (leaderboard != null)
                {
                    var context = CreateContext();
                    context["board"] = leaderboard;

                    return templateEngine.Render(context, "layout", "leaderboard");
                }

                return Error(templateEngine, "Could not find season: " + input);
            });

            server.Post("/nexus", (request) =>
            {
                if (!initialized)
                {
                    return HTTPResponse.Redirect("/nexus");
                }

                var input = request.GetVariable("searchInput");

                var results = nexus.SearchItem(input).Select(x => new SearchResultWithURL()
                {
                    Kind = x.Kind,
                    Text = x.Text,
                    URL = GetURLForSearch(x.Kind, x.Data)
                }).ToList();

                var context = CreateContext();

                context["results"] = results;

                return templateEngine.Render(context, "layout", "search");

                //return Error(templateEngine, "Could not find anything...");
            });

            bool running = true;

            new Thread(() =>
            {
                while (running)
                {
                    Thread.Sleep(1000 * 30);

                    try
                    {
                        if (initialized)
                        {
                            nexus.Update();
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Update() exception caught: " + e.ToString());
                    }
                }
            }).Start();

            Console.CancelKeyPress += delegate {
                Console.WriteLine("Terminating explorer...");
                running = false;
                try
                {
                    server.Stop();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }

                Environment.Exit(0);
            };


            Console.WriteLine($"Explorer running at port {settings.Port}");
            server.Run();
        }
    }
}
