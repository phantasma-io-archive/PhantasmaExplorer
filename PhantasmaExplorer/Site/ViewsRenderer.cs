using System;
using System.Collections.Generic;
using LunarLabs.WebServer.HTTP;
using LunarLabs.WebServer.Templates;
using Phantasma.Blockchain;
using Phantasma.Explorer.Controllers;

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
                new MenuContext {text = "Transactions", url = "/transactions", active = true},
                new MenuContext {text = "Chains", url = "/chains", active = false},
                new MenuContext {text = "Blocks", url = "/blocks", active = false},
                new MenuContext {text = "Tokens", url = "/tokens", active = false},
                new MenuContext {text = "Addresses", url = "/addresses", active = false}
            };

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

        public void SetupControllers(Nexus nexus) //todo this should be done by other class
        {
            AddressesController = new AddressesController(nexus);
            BlocksController = new BlocksController(nexus);
            ChainsController = new ChainsController(nexus);
            TransactionsController = new TransactionsController(nexus);
            TokensController = new TokensController(nexus);
        }

        public void SetupHandlers() //todo move magic strings to vars and separate each call
        {
            TemplateEngine.Site.Get("/", request => HTTPResponse.Redirect("/transactions"));

            //todo add error/empty view if object from controller call is null or empty
            TemplateEngine.Site.Get("/tokens", request =>
            {
                var tokensList = TokensController.GetTokens();

                UpdateContext("tokens", tokensList);
                return RendererView(new[] {"layout", "tokens"});
            });

            #region Transactions

            TemplateEngine.Site.Get("/transactions", request =>
            {
                var txList = TransactionsController.GetTransactions();

                UpdateContext("transactions", txList);
                return RendererView(new[] {"layout", "transactions"});
            });

            TemplateEngine.Site.Get("/tx/{input}", request =>
            {
                var txHash = request.GetVariable("input");
                var tx = TransactionsController.GetTransaction(txHash);

                UpdateContext("transaction", tx);

                return RendererView(new[] {"layout", "transaction"});
            });

            TemplateEngine.Site.Get("/txx/{input}", request =>
            {
                var input = request.GetVariable("input"); // todo ask why input = "block=xxxx"
                var txList = TransactionsController.GetTransactionsByBlock(input);
                UpdateContext("transactionsBlock", txList);

                return RendererView(new[] {"layout", "transactionsBlock"});
            });

            #endregion

            #region Address

            TemplateEngine.Site.Get("/addresses", request =>
            {
                var addressList = AddressesController.GetAddressList();

                UpdateContext("addresses", addressList);
                return RendererView(new[] {"layout", "addresses"});
            });

            TemplateEngine.Site.Get("/address/{input}", request =>
            {
                var addressText = request.GetVariable("input");
                var address = AddressesController.GetAddress(addressText);

                UpdateContext("address", address);
                return RendererView(new[] {"layout", "address"});
            });

            #endregion

            #region Blocks

            TemplateEngine.Site.Get("/blocks", request =>
            {
                var blocksList = BlocksController.GetLatestBlock();

                UpdateContext("blocks", blocksList);
                return RendererView(new[] {"layout", "blocks"});
            });

            TemplateEngine.Site.Get("/block/{input}", request => //input can be height or hash
            {
                var input = request.GetVariable("input");
                var block = BlocksController.GetBlock(input);

                UpdateContext("block", block);
                return RendererView(new[] {"layout", "block"});
            });

            #endregion

            #region Chains

            TemplateEngine.Site.Get("/chains", request =>
            {
                var chainList = ChainsController.GetChains();

                UpdateContext("chains", chainList);
                return RendererView(new[] {"layout", "chains"});
            });

            TemplateEngine.Site.Get("/chain/{input}",
                request => //todo this could be the name of the chain rather then the address?
                {
                    var addressText = request.GetVariable("input");
                    var chain = ChainsController.GetChain(addressText);
                    UpdateContext("chain", chain);
                    return RendererView(new[] {"layout", "chain"});
                });

            #endregion
        }

        public void RunServer()
        {
            TemplateEngine.Site.server.Run();
        }

        #region Controllers

        private AddressesController AddressesController { get; set; }
        private BlocksController BlocksController { get; set; }
        private ChainsController ChainsController { get; set; }
        private TransactionsController TransactionsController { get; set; }
        private TokensController TokensController { get; set; }

        #endregion
    }
}