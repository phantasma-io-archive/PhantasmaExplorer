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

        public Dictionary<string, object> Context { get; set; } = new Dictionary<string, object>();

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
            UpdateContext(errorContext, _errorContextInstance);
            UpdateContext(menuContext, _menus);
        }

        public string RendererView(params string[] templateList)
        {
            return TemplateEngine.Render(Context, templateList);
        }

        public void UpdateContext(string key, object value)
        {
            Context[key] = value;
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

        public void SetupHandlers() //todo separate each call
        {
            TemplateEngine.Site.Get("/", request => HTTPResponse.Redirect(urlHome));

            TemplateEngine.Site.Get(urlHome, request =>
            {
                var blocksAndTxs = HomeController.GetLastestInfo();

                UpdateContext(homeContext, blocksAndTxs);
                return RendererView("layout", homeContext);
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
                    UpdateContext(errorContext, _errorContextInstance);

                    return HTTPResponse.Redirect(urlError);
                }
            });

            TemplateEngine.Site.Get(urlError, request => RendererView(new[] { "layout", errorContext }));

            TemplateEngine.Site.Get(urlTokens, request =>
            {
                var tokensList = TokensController.GetTokens();
                if (tokensList != null && tokensList.Any())
                {
                    ActivateMenuItem(urlTokens);
                    UpdateContext(menuContext, _menus);
                    UpdateContext(tokensContext, tokensList);
                    return RendererView("layout", tokensContext);
                }

                _errorContextInstance.errorCode = "token error";
                _errorContextInstance.errorDescription = "Tokens not found";
                UpdateContext(errorContext, _errorContextInstance);

                return HTTPResponse.Redirect(urlError);
            });

            TemplateEngine.Site.Get($"{urlTokens}/{{input}}", request => //NFTs
            {
                var address = request.GetVariable("input");
                var nftList = TokensController.GetNftListByAddress(address);
                if (nftList != null && nftList.Any())
                {
                    UpdateContext(nftTokensContext, nftList);
                    return RendererView("layout", nftTokensContext);
                }

                _errorContextInstance.errorCode = "nft error";
                _errorContextInstance.errorDescription = $"no nfts found for this {address} address";
                UpdateContext(errorContext, _errorContextInstance);

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
                if (token != null)
                {
                    var holders = TokensController.GetHolders(token.Symbol);
                    var transfers = TokensController.GetTransfers(token.Symbol);

                    UpdateContext(tokenContext, token);
                    UpdateContext(holdersContext, holders);
                    UpdateContext("transfers", transfers);
                    return RendererView("layout", tokenContext, holdersContext);
                }

                _errorContextInstance.errorCode = "token error";
                _errorContextInstance.errorDescription = "Token not found";
                UpdateContext(errorContext, _errorContextInstance);

                return HTTPResponse.Redirect(urlError);
            });

            #region Transactions

            TemplateEngine.Site.Get(urlTransactions, request =>
            {
                var txList = TransactionsController.GetLastTransactions();
                if (txList.Count > 0)
                {
                    var test = _menus.SingleOrDefault(m => m.url == urlTransactions);
                    test.active = true;

                    UpdateContext(txsContext, txList);
                    return RendererView("layout", txsContext);
                }

                _errorContextInstance.errorCode = "txs error";
                _errorContextInstance.errorDescription = "No transactions found";
                UpdateContext(errorContext, _errorContextInstance);

                return HTTPResponse.Redirect(urlError);
            });

            TemplateEngine.Site.Get($"{urlTransaction}/{{input}}", request =>
            {
                var txHash = request.GetVariable("input");
                var tx = TransactionsController.GetTransaction(txHash);
                if (tx != null)
                {
                    ActivateMenuItem(urlTransaction);
                    UpdateContext(menuContext, _menus);
                    UpdateContext(txContext, tx);
                    return RendererView("layout", txContext);
                }

                _errorContextInstance.errorCode = "txs error";
                _errorContextInstance.errorDescription = $"Transaction {txHash} not found";
                UpdateContext(errorContext, _errorContextInstance);

                return HTTPResponse.Redirect(urlError);
            });

            TemplateEngine.Site.Get($"{urlTransactionInBlock}/{{input}}", request =>
            {
                var input = request.GetVariable("input");
                var txList = TransactionsController.GetTransactionsByBlock(input);
                if (txList.Count > 0)
                {
                    UpdateContext(txInBlockContext, txList);
                    UpdateContext("BlockHash", txList[0].Block.Hash);
                    return RendererView("layout", txInBlockContext);
                }
                _errorContextInstance.errorCode = "txs error";
                _errorContextInstance.errorDescription = $"No transactions found in {input} block";
                UpdateContext(errorContext, _errorContextInstance);

                return HTTPResponse.Redirect(urlError);
            });

            #endregion

            #region Address

            TemplateEngine.Site.Get($"{urlAddresses}", request =>
            {
                var addressList = AddressesController.GetAddressList();
                if (addressList != null && addressList.Any())
                {
                    ActivateMenuItem(urlAddresses);
                    UpdateContext(menuContext, _menus);
                    UpdateContext(addressesContext, addressList);
                    return RendererView("layout", addressesContext);
                }

                _errorContextInstance.errorCode = "Address error";
                _errorContextInstance.errorDescription = $"No addresses";
                UpdateContext(errorContext, _errorContextInstance);

                return HTTPResponse.Redirect(urlError);
            });

            TemplateEngine.Site.Get($"{urlAddress}/{{input}}", request =>
            {
                var addressText = request.GetVariable("input");
                var address = AddressesController.GetAddress(addressText);
                if (address != null)
                {
                    UpdateContext(addressContext, address);
                    return RendererView("layout", addressContext);
                }

                _errorContextInstance.errorCode = "Address error";
                _errorContextInstance.errorDescription = $"Invalid address";
                UpdateContext(errorContext, _errorContextInstance);

                return HTTPResponse.Redirect(urlError);
            });

            #endregion

            #region Blocks

            TemplateEngine.Site.Get($"{urlBlocks}", request =>
            {
                var blocksList = BlocksController.GetLatestBlocks();
                if (blocksList.Count > 0)
                {
                    ActivateMenuItem(urlBlocks);
                    UpdateContext(menuContext, _menus);
                    UpdateContext(blocksContext, blocksList);
                    return RendererView("layout", blocksContext);
                }

                _errorContextInstance.errorCode = "blocks error";
                _errorContextInstance.errorDescription = "No blocks found";
                UpdateContext(errorContext, _errorContextInstance);

                return HTTPResponse.Redirect(urlError);

            });

            TemplateEngine.Site.Get($"{urlBlock}/{{input}}", request => //input can be height or hash
            {
                var input = request.GetVariable("input");
                var block = BlocksController.GetBlock(input);
                if (block != null)
                {
                    UpdateContext(blockContext, block);
                    return RendererView("layout", blockContext);
                }

                _errorContextInstance.errorCode = "blocks error";
                _errorContextInstance.errorDescription = $"No block found with this {input} input";
                UpdateContext(errorContext, _errorContextInstance);

                return HTTPResponse.Redirect(urlError);
            });

            #endregion

            #region Chains

            TemplateEngine.Site.Get($"{urlChains}", request =>
            {
                var chainList = ChainsController.GetChains();
                if (chainList.Count > 0)
                {
                    ActivateMenuItem(urlChains);
                    UpdateContext(menuContext, _menus);
                    UpdateContext(chainsContext, chainList);
                    return RendererView("layout", chainsContext);
                }
                _errorContextInstance.errorCode = "chains error";
                _errorContextInstance.errorDescription = "No chains found";
                UpdateContext(errorContext, _errorContextInstance);

                return HTTPResponse.Redirect(urlError);
            });

            TemplateEngine.Site.Get($"{urlChain}/{{input}}",
                request =>
                {
                    var addressText = request.GetVariable("input");
                    var chain = ChainsController.GetChain(addressText);
                    if (chain != null)
                    {
                        UpdateContext(chainContext, chain);
                        return RendererView("layout", chainContext);
                    }

                    _errorContextInstance.errorCode = "chains error";
                    _errorContextInstance.errorDescription = $"No chain found with this {addressText} address";
                    UpdateContext(errorContext, _errorContextInstance);

                    return HTTPResponse.Redirect(urlError);
                });

            #endregion

            #region Apps
            TemplateEngine.Site.Get($"{urlApps}", request =>
            {

                var appList = AppsController.GetAllApps();
                if (appList.Count > 0)
                {
                    UpdateContext(appsContext, appList);
                    return RendererView("layout", appsContext);
                }
                _errorContextInstance.errorCode = "apps error";
                _errorContextInstance.errorDescription = "No apps found";
                UpdateContext(errorContext, _errorContextInstance);

                return HTTPResponse.Redirect(urlError);
            });

            TemplateEngine.Site.Get($"{urlApp}/{{input}}", request =>
            {
                var appId = request.GetVariable("input");

                var app = AppsController.GetApp(appId);
                if (app != null)
                {
                    UpdateContext(appContext, app);
                    return RendererView("layout", appContext);
                }

                _errorContextInstance.errorCode = "apps error";
                _errorContextInstance.errorDescription = $"No app with {appId} found";
                UpdateContext(errorContext, _errorContextInstance);

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
        private readonly string urlTransactionInBlock = "/txsblock";
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
        private readonly string txInBlockContext = "transactionsBlock";
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