using System;
using System.Collections.Generic;
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

        public void Init()
        {
            var menus = new List<MenuContext>
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
            Context["menu"] = menus;
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
        }

        public void SetupControllers(IRepository repo) //todo this should be done by other class
        {
            HomeController = new HomeController(repo);
            AddressesController = new AddressesController(repo);
            BlocksController = new BlocksController(repo);
            ChainsController = new ChainsController(repo);
            TransactionsController = new TransactionsController(repo);
            TokensController = new TokensController(repo);
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

                UpdateContext(tokensContext, tokensList);
                return RendererView("layout", tokensContext);
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

                var html = TemplateEngine.Render(coins,  new[] { "rates" });
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

                UpdateContext(addressesContext, addressList);
                return RendererView("layout", addressesContext);
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
                    UpdateContext(chainsContext, chainList);
                    return RendererView("layout", chainsContext);
                }
                _errorContextInstance.errorCode = "chains error";
                _errorContextInstance.errorDescription = "No chains found";
                UpdateContext(errorContext, _errorContextInstance);

                return HTTPResponse.Redirect(urlError);
            });

            TemplateEngine.Site.Get($"{urlChain}/{{input}}",
                request => //todo this could be the name of the chain rather then the address?
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
        }

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

        private readonly string homeContext = "home";
        private readonly string tokensContext = "tokens";
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
        private readonly string errorContext = "error";
        private readonly string holdersContext = "holders";

        public struct MenuContext
        {
            public string text;
            public string url;
            public bool active;
        }

        public struct ErrorContext //todo more info?
        {
            public string errorDescription;
            public string errorCode;
        }

        #endregion

        #region Controllers

        private HomeController HomeController { get; set; }
        private AddressesController AddressesController { get; set; }
        private BlocksController BlocksController { get; set; }
        private ChainsController ChainsController { get; set; }
        private TransactionsController TransactionsController { get; set; }
        private TokensController TokensController { get; set; }

        #endregion
    }
}