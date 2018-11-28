using System;
using System.Collections.Generic;
using System.Linq;
using LunarLabs.WebServer.HTTP;
using LunarLabs.WebServer.Templates;
using Phantasma.Explorer.Controllers;
using Phantasma.Explorer.Infrastructure.Interfaces;
using Phantasma.Explorer.Utils;

namespace Phantasma.Explorer.Site
{
    public class ViewsRenderer
    {
        public ViewsRenderer(LunarLabs.WebServer.Core.Site site, string viewsPath)
        {
            if (site == null) throw new ArgumentNullException(nameof(site));
            TemplateEngine = new TemplateEngine(site, viewsPath);
        }

        public TemplateEngine TemplateEngine { get; set; }

        private ErrorContext _errorContextInstance;
        private List<MenuContext> _menus;

        public void Init()
        {
            _menus = new List<MenuContext>
            {
                new MenuContext {text = "Transactions", url = urlTransactions, active = true},
                new MenuContext {text = "Chains", url = urlChains, active = false},
                new MenuContext {text = "Blocks", url = urlBlocks, active = false},
                new MenuContext {text = "Tokens", url = urlTokens, active = false},
                new MenuContext {text = "Addresses", url = urlAddresses, active = false},
                new MenuContext {text = "Apps", url = urlApps, active = false},
            };
            SetupTags();
            //UpdateContext(errorContext, _errorContextInstance);
            //UpdateContext(menuContext, _menus);
        }

        public string RendererView(Dictionary<string, object> context, params string[] templateList)
        {
            return TemplateEngine.Render(context, templateList);
        }

        private void SetupTags()
        {
            TemplateEngine.RegisterTag("value", (doc, val) => new ValueTag(doc, val));
            TemplateEngine.RegisterTag("timeago", (doc, val) => new TimeAgoTag(doc, val));
            TemplateEngine.RegisterTag("async", (doc, val) => new AsyncTag(doc, val));
            TemplateEngine.RegisterTag("link-chain", (doc, val) => new LinkChainTag(doc, val));
            TemplateEngine.RegisterTag("link-tx", (doc, val) => new LinkTransactionTag(doc, val));
            TemplateEngine.RegisterTag("link-address", (doc, val) => new LinkAddressTag(doc, val));
            TemplateEngine.RegisterTag("link-block", (doc, val) => new LinkBlockTag(doc, val));
            TemplateEngine.RegisterTag("description", (doc, val) => new DescriptionTag(doc, val));
            TemplateEngine.RegisterTag("link-app", (doc, val) => new LinkAppTag(doc, val));
            TemplateEngine.RegisterTag("appIcon", (doc, val) => new AppIconTag(doc, val));
            TemplateEngine.RegisterTag("externalLink", (doc, val) => new LinkExternalTag(doc, val));
        }

        public void SetupControllers(IRepository repo) //todo this should be done by other class
        {
            HomeController = new HomeController(repo);
            AddressesController = new AddressesController(repo);
            BlocksController = new BlocksController(repo);
            ChainsController = new ChainsController(repo);
            TransactionsController = new TransactionsController(repo);
            TokensController = new TokensController(repo);
            AppsController = new AppsController(repo);
            APIController = new APIController(repo);
        }

        private Dictionary<string, object> GetSessionContext(HTTPRequest request) => request.session.data.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        public void SetupHandlers() //todo separate each call
        {
            TemplateEngine.Site.Get("/", request =>
            {
                Init();
                return HTTPResponse.Redirect(urlHome);
            });

            TemplateEngine.Site.Get(urlHome, request =>
            {
                var context = GetSessionContext(request);
                var blocksAndTxs = HomeController.GetLastestInfo();
                context[menuContext] = _menus;
                context[homeContext] = blocksAndTxs;
                return RendererView(context, "layout", homeContext);
            });

            TemplateEngine.Site.Post(urlHome, request =>
            {
                try
                {
                    var searchInput = request.GetVariable("searchInput");
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
                    _errorContextInstance.errorCode = ex.Message;
                    _errorContextInstance.errorDescription = ex.StackTrace;
                    request.session.Set(errorContext, _errorContextInstance);

                    return HTTPResponse.Redirect(urlError);
                }
            });

            TemplateEngine.Site.Get(urlError, request =>
            {
                var error = request.session.Get<ErrorContext>("error");
                var context = GetSessionContext(request);
                context[menuContext] = _menus;
                context[errorContext] = error;
                return RendererView(context, (new[] { "layout", errorContext }));
            });

            TemplateEngine.Site.Get(urlTokens, request =>
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
                _errorContextInstance.errorCode = "token error";
                _errorContextInstance.errorDescription = "Tokens not found";
                request.session.Set(errorContext, _errorContextInstance);

                return HTTPResponse.Redirect(urlError);
            });

            TemplateEngine.Site.Get($"{urlTokens}/{{input}}", request => //NFTs
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

                _errorContextInstance.errorCode = "nft error";
                _errorContextInstance.errorDescription = $"no nfts found for this {address} address";
                context[errorContext] = _errorContextInstance;

                return HTTPResponse.Redirect(urlError);
            });

            TemplateEngine.Site.Get("/marketcap", request =>
            {
                var info = CoinUtils.GetCoinInfoAsync(CoinUtils.SoulId, "USD").Result;
                var marketCap = info["quotes"]["USD"].GetDecimal("market_cap");
                return $"${marketCap}";
            });

            TemplateEngine.Site.Get("/rates", request =>
            {
                var coins = HomeController.GetRateInfo();

                var html = TemplateEngine.Render(coins, new[] { "rates" });
                return html;
            });

            TemplateEngine.Site.Get($"{urlToken}/{{input}}", request =>
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
                _errorContextInstance.errorCode = "token error";
                _errorContextInstance.errorDescription = "Token not found";
                request.session.Set(errorContext, _errorContextInstance);

                return HTTPResponse.Redirect(urlError);
            });

            #region Transactions

            TemplateEngine.Site.Get(urlTransactions, request =>
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

                _errorContextInstance.errorCode = "txs error";
                _errorContextInstance.errorDescription = "No transactions found";
                request.session.Set(errorContext, _errorContextInstance);

                return HTTPResponse.Redirect(urlError);
            });

            TemplateEngine.Site.Get($"{urlTransaction}/{{input}}", request =>
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

                _errorContextInstance.errorCode = "txs error";
                _errorContextInstance.errorDescription = $"Transaction {txHash} not found";
                request.session.Set(errorContext, _errorContextInstance);

                return HTTPResponse.Redirect(urlError);
            });

            #endregion

            #region Address

            TemplateEngine.Site.Get($"{urlAddresses}", request =>
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

                _errorContextInstance.errorCode = "Address error";
                _errorContextInstance.errorDescription = $"No addresses";
                request.session.Set(errorContext, _errorContextInstance);

                return HTTPResponse.Redirect(urlError);
            });

            TemplateEngine.Site.Get($"{urlAddress}/{{input}}", request =>
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

                _errorContextInstance.errorCode = "Address error";
                _errorContextInstance.errorDescription = $"Invalid address";
                request.session.Set(errorContext, _errorContextInstance);

                return HTTPResponse.Redirect(urlError);
            });

            #endregion

            #region Blocks

            TemplateEngine.Site.Get($"{urlBlocks}", request =>
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

                _errorContextInstance.errorCode = "blocks error";
                _errorContextInstance.errorDescription = "No blocks found";
                request.session.Set(errorContext, _errorContextInstance);

                return HTTPResponse.Redirect(urlError);

            });

            TemplateEngine.Site.Get($"{urlBlock}/{{input}}", request => //input can be height or hash
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

                _errorContextInstance.errorCode = "blocks error";
                _errorContextInstance.errorDescription = $"No block found with this {input} input";
                request.session.Set(errorContext, _errorContextInstance);

                return HTTPResponse.Redirect(urlError);
            });

            #endregion

            #region Chains

            TemplateEngine.Site.Get($"{urlChains}", request =>
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
                _errorContextInstance.errorCode = "chains error";
                _errorContextInstance.errorDescription = "No chains found";
                request.session.Set(errorContext, _errorContextInstance);

                return HTTPResponse.Redirect(urlError);
            });

            TemplateEngine.Site.Get($"{urlChain}/{{input}}",
                request =>
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

                    _errorContextInstance.errorCode = "chains error";
                    _errorContextInstance.errorDescription = $"No chain found with this {addressText} address";
                    request.session.Set(errorContext, _errorContextInstance);

                    return HTTPResponse.Redirect(urlError);
                });

            #endregion

            #region Apps
            TemplateEngine.Site.Get($"{urlApps}", request =>
            {
                var appList = AppsController.GetAllApps();
                var context = GetSessionContext(request);
                if (appList.Count > 0)
                {
                    context[menuContext] = _menus;
                    context[appsContext] = appList;
                    return RendererView(context, "layout", appsContext);
                }
                _errorContextInstance.errorCode = "apps error";
                _errorContextInstance.errorDescription = "No apps found";
                request.session.Set(errorContext, _errorContextInstance);

                return HTTPResponse.Redirect(urlError);
            });

            TemplateEngine.Site.Get($"{urlApp}/{{input}}", request =>
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

                _errorContextInstance.errorCode = "apps error";
                _errorContextInstance.errorDescription = $"No app with {appId} found";
                request.session.Set(errorContext, _errorContextInstance);

                return HTTPResponse.Redirect(urlError);
            });

            SetupAPIHandlers();
        }
        #endregion

        #region API
        private void SetupAPIHandlers()
        {
            TemplateEngine.Site.Get($"{urlAPI}/get_account/{{address}}", request =>
            {
                var address = request.GetVariable("address");
                return APIController.GetAccount(address);
            });

            TemplateEngine.Site.Get($"{urlAPI}/get_block/{{blockHash}}", request =>
            {
                var address = request.GetVariable("blockHash");
                return APIController.GetBlock(address);
            });

            TemplateEngine.Site.Get($"{urlAPI}/get_block/{{chain}}/{{height}}", request =>
            {
                var chain = request.GetVariable("chain");
                var height = (uint.Parse(request.GetVariable("height")));
                return APIController.GetBlock(height, chain);
            });

            TemplateEngine.Site.Get($"{urlAPI}/get_account_txs/{{address}}/{{amount}}", request =>
            {
                var address = request.GetVariable("address");
                var amount = int.Parse(request.GetVariable("amount"));
                return APIController.GetAddressTransactions(address, amount);
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
            public string text { get; set; }
            public string url { get; set; }
            public bool active { get; set; }
        }

        public struct ErrorContext //todo more info?
        {
            public string errorDescription;
            public string errorCode;
        }

        private void ActivateMenuItem(string url)
        {
            _menus.ForEach(p => p.active = false);
            var menu = _menus.SingleOrDefault(p => p.url == url);
            if (menu != null) menu.active = true;
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
        private APIController APIController { get; set; }

        #endregion
    }
}