using System;
using System.Collections.Generic;
using System.Linq;
using LunarLabs.WebServer.HTTP;
using LunarLabs.WebServer.Templates;
using Microsoft.EntityFrameworkCore;
using Phantasma.Core;
using Phantasma.Explorer.Application;
using Phantasma.Explorer.Controllers;
using Phantasma.Explorer.Infrastructure.Interfaces;
using Phantasma.Explorer.Persistance;
using Phantasma.Explorer.Utils;

namespace Phantasma.Explorer.Site
{
    public class ViewsRenderer
    {
        public ViewsRenderer(HTTPServer server, string viewsPath)
        {
            Throw.IfNull(server, nameof(server));
            TemplateEngine = new TemplateEngine(server, viewsPath);
        }

        public TemplateEngine TemplateEngine { get; set; }

        private ErrorContext _errorContextInstance;
        private List<MenuContext> _menus;

        public void Init()
        {
            _menus = new List<MenuContext>
            {
                new MenuContext {Text = "Transactions", Url = urlTransactions, Active = true},
                new MenuContext {Text = "Chains", Url = urlChains, Active = false},
                new MenuContext {Text = "Blocks", Url = urlBlocks, Active = false},
                new MenuContext {Text = "Tokens", Url = urlTokens, Active = false},
                new MenuContext {Text = "Addresses", Url = urlAddresses, Active = false},
                new MenuContext {Text = "Apps", Url = urlApps, Active = false},
            };
            SetupTags();
        }

        public string RendererView(Dictionary<string, object> context, params string[] templateList)
        {
            return TemplateEngine.Render(context, templateList);
        }

        private void SetupTags()
        {
            TemplateEngine.Compiler.RegisterTag("value", (doc, val) => new ValueTag(doc, val));
            TemplateEngine.Compiler.RegisterTag("timeago", (doc, val) => new TimeAgoTag(doc, val));
            TemplateEngine.Compiler.RegisterTag("async", (doc, val) => new AsyncTag(doc, val));
            TemplateEngine.Compiler.RegisterTag("link-chain", (doc, val) => new LinkChainTag(doc, val));
            TemplateEngine.Compiler.RegisterTag("link-tx", (doc, val) => new LinkTransactionTag(doc, val));
            TemplateEngine.Compiler.RegisterTag("link-address", (doc, val) => new LinkAddressTag(doc, val));
            TemplateEngine.Compiler.RegisterTag("link-block", (doc, val) => new LinkBlockTag(doc, val));
            TemplateEngine.Compiler.RegisterTag("description", (doc, val) => new DescriptionTag(doc, val));
            TemplateEngine.Compiler.RegisterTag("link-app", (doc, val) => new LinkAppTag(doc, val));
            TemplateEngine.Compiler.RegisterTag("appIcon", (doc, val) => new AppIconTag(doc, val));
            TemplateEngine.Compiler.RegisterTag("externalLink", (doc, val) => new LinkExternalTag(doc, val));
        }

        public void SetupControllers(ExplorerDbContext context) //todo this should be done by other class
        {
            HomeController = new HomeController(context);
            AddressesController = new AddressesController(context);
            BlocksController = new BlocksController(context);
            ChainsController = new ChainsController(context);
            TransactionsController = new TransactionsController(context);
            TokensController = new TokensController(context);
            AppsController = new AppsController(context);
            ApiController = new ApiController(context);
        }

        private Dictionary<string, object> GetSessionContext(HTTPRequest request) => request.session.Data.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        public void SetupHandlers() //todo separate each call
        {
            TemplateEngine.Server.Get("/", request => HTTPResponse.Redirect(urlHome));

            TemplateEngine.Server.Get(urlHome, RouteHome);

            TemplateEngine.Server.Post(urlHome, RouteSearch);

            TemplateEngine.Server.Get(urlError, RouteError);

            TemplateEngine.Server.Get(urlTokens, RouteTokens);

            TemplateEngine.Server.Get($"{urlTokens}/{{input}}", RouteTokensNft);

            TemplateEngine.Server.Get("/marketcap", request =>
            {
                var info = CoinUtils.GetCoinInfoAsync(CoinUtils.SoulId).Result;
                var marketCap = info["quotes"]["USD"].GetDecimal("market_cap");
                return $"${marketCap}";
            });

            TemplateEngine.Server.Get("/rates", request =>
            {
                var coins = HomeController.GetRateInfo();

                var html = TemplateEngine.Render(coins, new[] { "rates" });
                return html;
            });

            TemplateEngine.Server.Get($"{urlToken}/{{input}}", RouteToken);

            TemplateEngine.Server.Get(urlTransactions, RouteTransactions);

            TemplateEngine.Server.Get($"{urlTransaction}/{{input}}", RouteTransaction);

            TemplateEngine.Server.Get($"{urlAddresses}", RouteAddresses);

            TemplateEngine.Server.Get($"{urlAddress}/{{input}}", RouteAddress);

            TemplateEngine.Server.Get($"{urlBlocks}", RouteBlocks);

            TemplateEngine.Server.Get($"{urlBlock}/{{input}}", RouteBlock);

            TemplateEngine.Server.Get($"{urlChains}", RouteChains);

            TemplateEngine.Server.Get($"{urlChain}/{{input}}", RouteChain);

            TemplateEngine.Server.Get($"{urlApps}", RouteApps);

            TemplateEngine.Server.Get($"{urlApp}/{{input}}", RouteApp);

            //SetupAPIHandlers(); todo
        }
        #region ROUTES

        private object RouteHome(HTTPRequest request)
        {
            var context = GetSessionContext(request);
            var blocksAndTxs = HomeController.GetLastestInfo();
            context[menuContext] = _menus;
            context[homeContext] = blocksAndTxs;
            return RendererView(context, "layout", homeContext);
        }

        private object RouteSearch(HTTPRequest request)
        {
            try
            {
                var searchInput = request.GetVariable("searchInput").Trim();
                if (!string.IsNullOrEmpty(searchInput))
                {
                    var url = HomeController.SearchCommand(searchInput);
                    if (!string.IsNullOrEmpty(url))
                    {
                        return HTTPResponse.Redirect(url);
                    }
                }
                return HTTPResponse.Redirect(urlHome);
            }
            catch (Exception ex)
            {
                _errorContextInstance.ErrorCode = ex.Message;
                _errorContextInstance.ErrorDescription = ex.StackTrace;
                request.session.SetStruct<ErrorContext>(errorContext, _errorContextInstance);

                return HTTPResponse.Redirect(urlError);
            }
        }

        private object RouteError(HTTPRequest request)
        {
            var error = request.session.GetStruct<ErrorContext>("error");
            var context = GetSessionContext(request);
            context[menuContext] = _menus;
            context[errorContext] = error;
            return RendererView(context, (new[] { "layout", errorContext }));
        }

        private object RouteTokens(HTTPRequest request)
        {
            var tokensList = TokensController.GetTokens();
            var temp = tokensList.SingleOrDefault(t => t.Name == "Trophy");
            var context = GetSessionContext(request);
            tokensList.Remove(temp);
            if (tokensList.Any())
            {
                ActivateMenuItem(urlTokens);

                context[menuContext] = _menus;
                context[tokensContext] = tokensList;
                return RendererView(context, "layout", tokensContext);
            }
            _errorContextInstance.ErrorCode = "token error";
            _errorContextInstance.ErrorDescription = "Tokens not found";
            request.session.SetStruct<ErrorContext>(errorContext, _errorContextInstance);

            return HTTPResponse.Redirect(urlError);
        }

        private object RouteTokensNft(HTTPRequest request)
        {
            var address = request.GetVariable("input");
            var nftList = TokensController.GetNftListByAddress(address);
            var context = GetSessionContext(request);
            if (nftList != null && nftList.Any())
            {
                context[menuContext] = _menus;
                context[nftTokensContext] = nftList;
                return RendererView(context, "layout", nftTokensContext);
            }

            _errorContextInstance.ErrorCode = "nft error";
            _errorContextInstance.ErrorDescription = $"no nfts found for this {address} address";
            context[errorContext] = _errorContextInstance;

            return HTTPResponse.Redirect(urlError);
        }

        private object RouteToken(HTTPRequest request)
        {
            var tokenSymbol = request.GetVariable("input");
            var token = TokensController.GetToken(tokenSymbol);
            var context = GetSessionContext(request);
            if (token != null)
            {
                var holders = TokensController.GetHolders(token.Symbol);
                var transfers = TokensController.GetTransfers(token.Symbol);

                context[menuContext] = _menus;
                context[tokenContext] = token;
                context[holdersContext] = holders;
                context["transfers"] = transfers;
                return RendererView(context, "layout", tokenContext, holdersContext, "transfers");
            }
            _errorContextInstance.ErrorCode = "token error";
            _errorContextInstance.ErrorDescription = "Token not found";
            request.session.SetStruct<ErrorContext>(errorContext, _errorContextInstance);

            return HTTPResponse.Redirect(urlError);
        }

        private object RouteTransactions(HTTPRequest request)
        {
            var txList = TransactionsController.GetLastTransactions();
            var context = GetSessionContext(request);
            if (txList.Count > 0)
            {
                ActivateMenuItem(urlTransactions);

                context[menuContext] = _menus;
                context[txsContext] = txList;
                return RendererView(context, "layout", txsContext);
            }

            _errorContextInstance.ErrorCode = "txs error";
            _errorContextInstance.ErrorDescription = "No transactions found";
            request.session.SetStruct<ErrorContext>(errorContext, _errorContextInstance);

            return HTTPResponse.Redirect(urlError);
        }

        private object RouteTransaction(HTTPRequest request)
        {
            var txHash = request.GetVariable("input");
            var tx = TransactionsController.GetTransaction(txHash);
            var context = GetSessionContext(request);
            if (tx != null)
            {
                ActivateMenuItem(urlTransaction);

                context[menuContext] = _menus;
                context[txContext] = tx;

                return RendererView(context, "layout", txContext);
            }

            _errorContextInstance.ErrorCode = "txs error";
            _errorContextInstance.ErrorDescription = $"Transaction {txHash} not found";
            request.session.SetStruct<ErrorContext>(errorContext, _errorContextInstance);

            return HTTPResponse.Redirect(urlError);
        }

        private object RouteAddresses(HTTPRequest request)
        {
            var addressList = AddressesController.GetAddressList();
            var context = GetSessionContext(request);
            if (addressList != null && addressList.Any())
            {
                ActivateMenuItem(urlAddresses);
                context[menuContext] = _menus;
                context[addressesContext] = addressList;

                return RendererView(context, "layout", addressesContext);
            }

            _errorContextInstance.ErrorCode = "Address error";
            _errorContextInstance.ErrorDescription = $"No addresses";
            request.session.SetStruct<ErrorContext>(errorContext, _errorContextInstance);

            return HTTPResponse.Redirect(urlError);
        }

        private object RouteAddress(HTTPRequest request)
        {

            var addressText = request.GetVariable("input");
            var address = AddressesController.GetAddress(addressText);
            var context = GetSessionContext(request);
            if (address != null)
            {
                context[menuContext] = _menus;
                context[addressContext] = address;
                return RendererView(context, "layout", addressContext);
            }

            _errorContextInstance.ErrorCode = "Address error";
            _errorContextInstance.ErrorDescription = $"Invalid address";
            request.session.SetStruct<ErrorContext>(errorContext, _errorContextInstance);

            return HTTPResponse.Redirect(urlError);
        }

        private object RouteBlocks(HTTPRequest request)
        {
            var blocksList = BlocksController.GetLatestBlocks();
            var context = GetSessionContext(request);
            if (blocksList.Count > 0)
            {
                ActivateMenuItem(urlBlocks);
                context[menuContext] = _menus;
                context[blocksContext] = blocksList;
                return RendererView(context, "layout", blocksContext);
            }

            _errorContextInstance.ErrorCode = "blocks error";
            _errorContextInstance.ErrorDescription = "No blocks found";
            request.session.SetStruct<ErrorContext>(errorContext, _errorContextInstance);

            return HTTPResponse.Redirect(urlError);
        }

        private object RouteBlock(HTTPRequest request)
        {
            var input = request.GetVariable("input");
            var block = BlocksController.GetBlock(input);
            var context = GetSessionContext(request);
            if (block != null)
            {
                context[menuContext] = _menus;
                context[blockContext] = block;
                return RendererView(context, "layout", blockContext);
            }

            _errorContextInstance.ErrorCode = "blocks error";
            _errorContextInstance.ErrorDescription = $"No block found with this {input} input";
            request.session.SetStruct<ErrorContext>(errorContext, _errorContextInstance);

            return HTTPResponse.Redirect(urlError);
        }

        private object RouteChains(HTTPRequest request)
        {
            var chainList = ChainsController.GetChains();
            var context = GetSessionContext(request);
            if (chainList.Count > 0)
            {
                ActivateMenuItem(urlChains);
                context[menuContext] = _menus;
                context[chainsContext] = chainList;

                return RendererView(context, "layout", chainsContext);
            }
            _errorContextInstance.ErrorCode = "chains error";
            _errorContextInstance.ErrorDescription = "No chains found";
            request.session.SetStruct<ErrorContext>(errorContext, _errorContextInstance);

            return HTTPResponse.Redirect(urlError);
        }

        private object RouteChain(HTTPRequest request)
        {
            var addressText = request.GetVariable("input");
            var chain = ChainsController.GetChain(addressText);
            var context = GetSessionContext(request);
            if (chain != null)
            {
                context[menuContext] = _menus;
                context[chainContext] = chain;

                return RendererView(context, "layout", chainContext);
            }

            _errorContextInstance.ErrorCode = "chains error";
            _errorContextInstance.ErrorDescription = $"No chain found with this {addressText} address";
            request.session.SetStruct<ErrorContext>(errorContext, _errorContextInstance);

            return HTTPResponse.Redirect(urlError);
        }

        private object RouteApps(HTTPRequest request)
        {
            var appList = AppsController.GetAllApps();
            var context = GetSessionContext(request);
            if (appList.Count > 0)
            {
                context[menuContext] = _menus;
                context[appsContext] = appList;
                return RendererView(context, "layout", appsContext);
            }
            _errorContextInstance.ErrorCode = "apps error";
            _errorContextInstance.ErrorDescription = "No apps found";
            request.session.SetStruct<ErrorContext>(errorContext, _errorContextInstance);

            return HTTPResponse.Redirect(urlError);
        }
        private object RouteApp(HTTPRequest request)
        {
            var appId = request.GetVariable("input");
            var app = AppsController.GetApp(appId);
            var context = GetSessionContext(request);
            if (app != null)
            {
                context[menuContext] = _menus;
                context[appContext] = app;

                return RendererView(context, "layout", appContext);
            }

            _errorContextInstance.ErrorCode = "apps error";
            _errorContextInstance.ErrorDescription = $"No app with {appId} found";
            request.session.SetStruct<ErrorContext>(errorContext, _errorContextInstance);

            return HTTPResponse.Redirect(urlError);
        }
        #endregion

        #region API
        private void SetupAPIHandlers()
        {
            TemplateEngine.Server.Get($"{urlAPI}/get_account/{{address}}", request =>
            {
                var address = request.GetVariable("address");
                return ApiController.GetAccount(address);
            });

            TemplateEngine.Server.Get($"{urlAPI}/get_account_txs/{{address}}/{{amount}}", request =>
            {
                var address = request.GetVariable("address");
                var amount = int.Parse(request.GetVariable("amount"));
                return ApiController.GetAddressTransactions(address, amount);
            });

            TemplateEngine.Server.Get($"{urlAPI}/get_apps", request => ApiController.GetApps());

            TemplateEngine.Server.Get($"{urlAPI}/get_block_height/{{chain}}", request =>
            {
                var chain = request.GetVariable("chain");
                return ApiController.GetBlockHeight(chain);
            });

            TemplateEngine.Server.Get($"{urlAPI}/get_block/{{blockHash}}", request =>
            {
                var address = request.GetVariable("blockHash");
                return ApiController.GetBlockByHash(address);
            });

            TemplateEngine.Server.Get($"{urlAPI}/get_block/{{chain}}/{{height}}", request =>
            {
                var chain = request.GetVariable("chain");
                var height = (uint.Parse(request.GetVariable("height")));
                return ApiController.GetBlockByHeight(chain, height);
            });

            TemplateEngine.Server.Get($"{urlAPI}/get_block_tx_count_by_hash/{{blockHash}}", request =>
            {
                var block = request.GetVariable("blockHash");
                return ApiController.GetBlockTransactionCountByHash(block);
            });

            TemplateEngine.Server.Get($"{urlAPI}/get_confirmations/{{txHash}}", request =>
            {
                var txHash = request.GetVariable("txHash");
                return ApiController.GetConfirmations(txHash);
            });

            TemplateEngine.Server.Get($"{urlAPI}/get_tx_by_block_hash_index/{{block}}/{{index}}", request =>
            {
                var block = request.GetVariable("block");
                var index = int.Parse(request.GetVariable("index"));
                return ApiController.GetTransactionByBlockHashAndIndex(block, index);
            });

            TemplateEngine.Server.Get($"{urlAPI}/get_chains", request => ApiController.GetChains());

            TemplateEngine.Server.Get($"{urlAPI}/get_tokens", request => ApiController.GetTokens());

            //todo confirm this
            TemplateEngine.Server.Post($"{urlAPI}/send_raw_tx/{{signedTx}}", request =>
            {
                var signedTx = request.GetVariable("signedTx");
                return ApiController.SendRawTransaction(signedTx);
            });
        }

        #endregion

        #region URL&CONTEXT

        private readonly string urlHome = "/home";
        private readonly string urlTokens = "/tokens";
        private readonly string urlToken = "/token";
        private readonly string urlTransactions = "/transactions";
        private readonly string urlTransaction = "/tx";
        private readonly string urlChains = "/chains";
        private readonly string urlChain = "/chain";
        private readonly string urlBlocks = "/blocks";
        private readonly string urlBlock = "/block";
        private readonly string urlAddresses = "/addresses";
        private readonly string urlAddress = "/address";
        private readonly string urlApps = "/apps";
        private readonly string urlApp = "/app";
        private readonly string urlError = "/error";
        private readonly string urlAPI = "/api";

        private readonly string homeContext = "home";
        private readonly string menuContext = "menu";
        private readonly string tokensContext = "tokens";
        private readonly string nftTokensContext = "nftTokens";
        private readonly string tokenContext = "token";
        private readonly string txContext = "transaction";
        private readonly string txsContext = "transactions";
        private readonly string addressesContext = "addresses";
        private readonly string addressContext = "address";
        private readonly string blocksContext = "blocks";
        private readonly string blockContext = "block";
        private readonly string chainsContext = "chains";
        private readonly string chainContext = "chain";
        private readonly string appsContext = "apps";
        private readonly string appContext = "app";
        private readonly string errorContext = "error";
        private readonly string holdersContext = "holders";

        public class MenuContext
        {
            public string Text { get; set; }
            public string Url { get; set; }
            public bool Active { get; set; }
        }

        public struct ErrorContext //todo more info?
        {
            public string ErrorDescription;
            public string ErrorCode;
        }

        private void ActivateMenuItem(string url)
        {
            _menus.ForEach(p => p.Active = false);
            var menu = _menus.SingleOrDefault(p => p.Url == url);
            if (menu != null) menu.Active = true;
        }

        #endregion

        #region Controllers

        private HomeController HomeController { get; set; }
        private AddressesController AddressesController { get; set; }
        private BlocksController BlocksController { get; set; }
        private ChainsController ChainsController { get; set; }
        private TransactionsController TransactionsController { get; set; }
        private TokensController TokensController { get; set; }
        private AppsController AppsController { get; set; }
        private ApiController ApiController { get; set; }

        #endregion
    }
}