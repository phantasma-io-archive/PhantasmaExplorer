using System;
using System.Collections.Generic;
using LunarLabs.WebServer.HTTP;
using LunarLabs.WebServer.Templates;
using Phantasma.Explorer.Controllers;
using Phantasma.Explorer.Infrastructure.Interfaces;

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

        public void InitMenus()
        {
            var menus = new List<MenuContext>
            {
                new MenuContext {text = "Transactions", url = urlTransactions, active = true},
                new MenuContext {text = "Chains", url = urlChains, active = false},
                new MenuContext {text = "Blocks", url = urlBlocks, active = false},
                new MenuContext {text = "Tokens", url = urlTokens, active = false},
                //new MenuContext {text = "Addresses", url = urlAddresses, active = false}
            };
            TemplateEngine.RegisterTag("value", (doc, val) => new PriceTag(doc, val));

            Context["menu"] = menus;
        }

        public string RendererView(IEnumerable<string> templateList)
        {
            return TemplateEngine.Render(Context, templateList);
        }

        public void UpdateContext(string key, object value)
        {
            Context[key] = value;
        }

        public void SetupControllers(IRepository repo) //todo this should be done by other class
        {
            AddressesController = new AddressesController(repo);
            BlocksController = new BlocksController(repo);
            ChainsController = new ChainsController(repo);
            TransactionsController = new TransactionsController(repo);
            TokensController = new TokensController(repo);
        }

        public void SetupHandlers() //todo move magic strings to vars and separate each call
        {
            TemplateEngine.Site.Get("/", request => HTTPResponse.Redirect(urlTransactions));

            //todo add error/empty view if object from controller call is null or empty
            TemplateEngine.Site.Get(urlTokens, request =>
            {
                var tokensList = TokensController.GetTokens();

                UpdateContext(tokensContext, tokensList);
                return RendererView(new[] { "layout", tokensContext });
            });

            TemplateEngine.Site.Get($"{urlToken}/{{input}}", request =>
            {
                var tokenSymbol = request.GetVariable("input");
                var token = TokensController.GetToken(tokenSymbol);

                UpdateContext(tokenContext, token);
                return RendererView(new[] { "layout", tokenContext });
            });

            #region Transactions

            TemplateEngine.Site.Get(urlTransactions, request =>
            {
                var txList = TransactionsController.GetLastTransactions();

                UpdateContext(txsContext, txList);
                return RendererView(new[] { "layout", txsContext });
            });

            TemplateEngine.Site.Get($"{urlTransaction}/{{input}}", request =>
            {
                var txHash = request.GetVariable("input");
                var tx = TransactionsController.GetTransaction(txHash);

                UpdateContext(txContext, tx);

                return RendererView(new[] { "layout", txContext });
            });

            TemplateEngine.Site.Get($"{urlTransactionInBlock}/{{input}}", request =>
            {
                var input = request.GetVariable("input");
                var txList = TransactionsController.GetTransactionsByBlock(input);
                UpdateContext(txInBlockContext, txList);

                return RendererView(new[] { "layout", txInBlockContext });
            });

            #endregion

            #region Address

            TemplateEngine.Site.Get($"{urlAddresses}", request =>
            {
                var addressList = AddressesController.GetAddressList();

                UpdateContext(addressesContext, addressList);
                return RendererView(new[] { "layout", addressesContext });
            });

            TemplateEngine.Site.Get($"{urlAddress}/{{input}}", request =>
            {
                var addressText = request.GetVariable("input");
                var address = AddressesController.GetAddress(addressText);

                UpdateContext(addressContext, address);
                return RendererView(new[] { "layout", addressContext });
            });

            #endregion

            #region Blocks

            TemplateEngine.Site.Get($"{urlBlocks}", request =>
            {
                var blocksList = BlocksController.GetLatestBlocks();

                UpdateContext(blocksContext, blocksList);
                return RendererView(new[] { "layout", blocksContext });
            });

            TemplateEngine.Site.Get($"{urlBlock}/{{input}}", request => //input can be height or hash
            {
                var input = request.GetVariable("input");
                var block = BlocksController.GetBlock(input);

                UpdateContext(blockContext, block);
                return RendererView(new[] { "layout", blockContext });
            });

            #endregion

            #region Chains

            TemplateEngine.Site.Get($"{urlChains}", request =>
            {
                var chainList = ChainsController.GetChains();

                UpdateContext(chainsContext, chainList);
                return RendererView(new[] { "layout", chainsContext });
            });

            TemplateEngine.Site.Get($"{urlChain}/{{input}}",
                request => //todo this could be the name of the chain rather then the address?
                {
                    var addressText = request.GetVariable("input");
                    var chain = ChainsController.GetChain(addressText);
                    UpdateContext(chainContext, chain);
                    return RendererView(new[] { "layout", chainContext });
                });

            #endregion
        }

        #region URL&CONTEXT

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

        #endregion

        #region Controllers

        private AddressesController AddressesController { get; set; }
        private BlocksController BlocksController { get; set; }
        private ChainsController ChainsController { get; set; }
        private TransactionsController TransactionsController { get; set; }
        private TokensController TokensController { get; set; }

        #endregion
    }
}