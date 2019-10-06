using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LunarLabs.WebServer.Core;
using LunarLabs.WebServer.HTTP;
using LunarLabs.WebServer.Templates;
using Phantasma.Core.Utils;
using Phantasma.Core.Types;
using Phantasma.Cryptography;
using Phantasma.Explorer;
using Phantasma.Numerics;
using Phantasma.Domain;
using Phantasma.VM;

namespace PhantasmaExplorer
{
    public struct MenuContext
    {
        public string text;
        public string url;
        public bool active;
    }

    public struct EventContext
    {
        public EventKind kind;
        public string content;
    }

    public struct TransactionContext
    {
        public string hash;
        public IBlock block;
        public DateTime date;
        public string chainName;
        public string chainAddress;
        public string fromName;
        public string fromAddress;
        public IEnumerable<EventContext> events;
        public IEnumerable<Instruction> instructions;

        /*private static string GetChainName(Nexus nexus, Address chainAddress)
        {
            var chain = nexus.FindChainByAddress(chainAddress);
            if (chain != null)
            {
                return chain.Name;
            }

            return "???";
        }*/

        // TODO exception and error handling
        private static string GetEventContent(Nexus nexus, Block block, Event evt)
        {
            switch (evt.Kind)
            {
                case EventKind.ChainCreate:
                    {
                        var chainAddress = Serialization.Unserialize<Address>(evt.Data);
                        var chain = nexus.FindChainByAddress(chainAddress);
                        return $"{chain.Name} chain created at address <a href=\"/chain/{chainAddress}\">{chainAddress}</a>.";
                    }

                case EventKind.TokenCreate:
                    {
                        var symbol = Serialization.Unserialize<string>(evt.Data);
                        var token = nexus.FindTokenBySymbol(symbol);
                        return $"{token.Name} token created with symbol <a href=\"/token/{symbol}\">{symbol}</a>.";
                    }

                case EventKind.TokenMint:
                case EventKind.TokenBurn:
                case EventKind.TokenSend:
                case EventKind.TokenReceive:
                    {
                        var data = Serialization.Unserialize<TokenEventData>(evt.Data);
                        var token = nexus.FindTokenBySymbol(data.symbol);
                        string action;

                        switch (evt.Kind)
                        {
                            case EventKind.TokenMint: action = "minted"; break;
                            case EventKind.TokenBurn: action = "burned"; break;
                            case EventKind.TokenSend: action = "sent"; break;
                            case EventKind.TokenReceive: action = "received"; break;
                            default: action = "???"; break;
                        }

                        string chainText;

                        if (data.chainAddress != block.Chain.Address)
                        {
                            Address srcAddress, dstAddress;

                            if (evt.Kind == EventKind.TokenReceive)
                            {
                                srcAddress = data.chainAddress;
                                dstAddress = block.Chain.Address;
                            }
                            else
                            {
                                srcAddress = block.Chain.Address;
                                dstAddress = data.chainAddress;
                            }

                            chainText = $"from <a href=\"/chain/{srcAddress}\">{GetChainName(nexus, srcAddress)} chain</a> to <a href=\"/chain/{dstAddress}\">{GetChainName(nexus, dstAddress)} chain";
                        }
                        else
                        {
                            chainText = $"in <a href=\"/chain/{data.chainAddress}\">{GetChainName(nexus, data.chainAddress)} chain";
                        }

                        return $"{UnitConversion.ToDecimal(data.amount)} {token.Name} tokens {action} at </a> address <a href=\"/address/{evt.Address}\">{evt.Address}</a> {chainText}.";
                    }

                default: return "Nothing.";
            }
        }

        public static TransactionContext FromTransaction(Nexus nexus, Block block, Transaction tx)
        {
            var evts = new List<EventContext>();
            foreach (var evt in tx.Events)
            {
                evts.Add(new EventContext()
                {
                    kind = evt.Kind,
                    content = GetEventContent(nexus, block, evt),
                });
            }

            var disasm = new Disassembler(tx.Script);

            return new TransactionContext()
            {
                block = block,
                chainAddress = block.Chain.Address.Text,
                chainName = block.Chain.Name,
                date = block.Timestamp,
                hash = tx.Hash.ToString(),
                fromAddress = "????",
                fromName = "Anonymous",
                events = evts,
                instructions = disasm.GetInstructions(),
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
                height = (int)block.Height,
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

        public static AddressContext FromAddress(Nexus nexus, Address address, List<TransactionContext> txList)
        {
            var balance = UnitConversion.ToDecimal(nexus.RootChain.GetTokenBalance(nexus.NativeToken, address));
            return new AddressContext()
            {
                address = address.Text,
                name = "Anonymous",
                balance = balance,
                value = balance * Explorer.soulRate,
                transactions = txList
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
        public static Database Database { get; private set; }

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
    
            // TODO this should be updated every 5 minutes or so
            soulRate = CoinUtils.GetCoinRate(2827);

            Database = new Database();

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

                addressList.Add(AddressContext.FromAddress(nexus, ownerKey.Address, null));
                addressList.Add(AddressContext.FromAddress(nexus, targetAddress, null));

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
                        hash = "mock",
                        chainName = nexusChain.Name,
                    });
                }

                var addressDto = AddressContext.FromAddress(nexus, address, mockTransactionList);

                var context = CreateContext();

                context["address"] = addressDto;

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
                var txHash = request.GetVariable("input");
                var hash = Hash.Parse(txHash);


                Transaction tx = null;
                Chain targetChain = null;
                foreach (var chain in nexus.Chains)
                {
                    tx = chain.FindTransaction(hash);
                    if (tx != null)
                    {
                        targetChain = chain;
                        break;
                    }
                }

                var block = targetChain.FindTransactionBlock(tx);

                var context = CreateContext();
                context["transaction"] = TransactionContext.FromTransaction(nexus, block, tx);
                return templateEngine.Render(site, context, new string[] { "layout", "transaction" });
            });

            // TODO change url
            site.Get("/txx/{input}", (request) =>
            {
                var input = request.GetVariable("input");// todo ask why input = "block=xxxx"
                var blockHash = Hash.Parse(input);
                Block block = null;
                var txList = new List<TransactionContext>();

                foreach (var chain in nexus.Chains)
                {
                    var x = chain.FindBlock(blockHash);
                    if (x != null)
                    {
                        block = x;
                        break;
                    }
                }

                if (block != null)
                {
                    foreach (var transaction in block.Transactions)
                    {
                        var tx = (Transaction)transaction;
                        txList.Add(TransactionContext.FromTransaction(nexus, block, tx));
                    }
                }

                var context = CreateContext();
                context["transactions"] = txList;
                context["blockheight"] = (int)block?.Height;
                return templateEngine.Render(site, context, new string[] { "layout", "transactionsBlock" });
            });

            site.Get("/block/{input}", (request) => //input can be height or hash
            {
                var input = request.GetVariable("input");
                Block block = null;
                if (int.TryParse(input, out var height))
                {
                    block = nexus.RootChain.FindBlock(height);
                }
                else
                {
                    var blockHash = (Hash.Parse(input));
                    foreach (var chain in nexus.Chains)
                    {
                        var x = chain.FindBlock(blockHash);
                        if (x != null)
                        {
                            block = x;
                            break;
                        }
                    }
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

                tempList = tempList.OrderBy(block => block.Timestamp.Value).ToList();
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
