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
                new MenuContext {Text = "Transactions", Url = AppSettings.UrlTransactions, Active = true},
                new MenuContext {Text = "Chains", Url = AppSettings.UrlChains, Active = false},
                new MenuContext {Text = "Blocks", Url = AppSettings.UrlBlocks, Active = false},
                new MenuContext {Text = "Tokens", Url = AppSettings.UrlTokens, Active = false},
                new MenuContext {Text = "Addresses", Url = AppSettings.UrlAddresses, Active = false},
                new MenuContext {Text = "Apps", Url = AppSettings.UrlApps, Active = false},
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

        public void SetupControllers() //todo this should be done by other class
        {
            HomeController = new HomeController();
            AddressesController = new AddressesController();
            BlocksController = new BlocksController();
            ChainsController = new ChainsController();
            TransactionsController = new TransactionsController();
            TokensController = new TokensController();
            AppsController = new AppsController();
            ApiController = new ApiController();
        }

        private Dictionary<string, object> GetSessionContext(HTTPRequest request) => request.session.Data.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        public void SetupHandlers() //todo separate each call
        {
            TemplateEngine.Server.Get("/", request => HTTPResponse.Redirect(AppSettings.UrlHome));

            TemplateEngine.Server.Get(AppSettings.UrlHome, RouteHome);

            TemplateEngine.Server.Post(AppSettings.UrlHome, RouteSearch);

            TemplateEngine.Server.Get(AppSettings.UrlError, RouteError);

            TemplateEngine.Server.Get(AppSettings.UrlTokens, RouteTokens);

            TemplateEngine.Server.Get($"{AppSettings.UrlTokens}/{{input}}", RouteTokensNft);

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

            TemplateEngine.Server.Get($"{AppSettings.UrlToken}/{{input}}", RouteToken);

            TemplateEngine.Server.Get(AppSettings.UrlTransactions, RouteTransactions);

            TemplateEngine.Server.Get($"{AppSettings.UrlTransaction}/{{input}}", RouteTransaction);

            TemplateEngine.Server.Get($"{AppSettings.UrlAddresses}", RouteAddresses);

            TemplateEngine.Server.Get($"{AppSettings.UrlAddress}/{{input}}", RouteAddress);

            TemplateEngine.Server.Get($"{AppSettings.UrlBlocks}", RouteBlocks);
            TemplateEngine.Server.Get($"{AppSettings.UrlBlocks}?p={{page}}", RouteBlocks);

            TemplateEngine.Server.Get($"{AppSettings.UrlBlock}/{{input}}", RouteBlock);

            TemplateEngine.Server.Get($"{AppSettings.UrlChains}", RouteChains);

            TemplateEngine.Server.Get($"{AppSettings.UrlChain}/{{input}}", RouteChain);

            TemplateEngine.Server.Get($"{AppSettings.UrlApps}", RouteApps);

            TemplateEngine.Server.Get($"{AppSettings.UrlApp}/{{input}}", RouteApp);

            //SetupAPIHandlers(); todo
        }
        #region ROUTES

        private object RouteHome(HTTPRequest request)
        {
            var context = GetSessionContext(request);
            var blocksAndTxs = HomeController.GetLastestInfo();
            context[AppSettings.MenuContext] = _menus;
            context[AppSettings.HomeContext] = blocksAndTxs;
            return RendererView(context, "layout", AppSettings.HomeContext);
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
                return HTTPResponse.Redirect(AppSettings.UrlHome);
            }
            catch (Exception ex)
            {
                _errorContextInstance.ErrorCode = ex.Message;
                _errorContextInstance.ErrorDescription = ex.StackTrace;
                request.session.SetStruct<ErrorContext>(AppSettings.ErrorContext, _errorContextInstance);

                return HTTPResponse.Redirect(AppSettings.UrlError);
            }
        }

        private object RouteError(HTTPRequest request)
        {
            var error = request.session.GetStruct<ErrorContext>("error");
            var context = GetSessionContext(request);
            context[AppSettings.MenuContext] = _menus;
            context[AppSettings.ErrorContext] = error;
            return RendererView(context, (new[] { "layout", AppSettings.ErrorContext }));
        }

        private object RouteTokens(HTTPRequest request)
        {
            var tokensList = TokensController.GetTokens();
            var temp = tokensList.SingleOrDefault(t => t.Name == "Trophy");
            var context = GetSessionContext(request);
            tokensList.Remove(temp);
            if (tokensList.Any())
            {
                ActivateMenuItem(AppSettings.UrlTokens);

                context[AppSettings.MenuContext] = _menus;
                context[AppSettings.TokensContext] = tokensList;
                return RendererView(context, "layout", AppSettings.TokensContext);
            }
            _errorContextInstance.ErrorCode = "token error";
            _errorContextInstance.ErrorDescription = "Tokens not found";
            request.session.SetStruct<ErrorContext>(AppSettings.ErrorContext, _errorContextInstance);

            return HTTPResponse.Redirect(AppSettings.UrlError);
        }

        private object RouteTokensNft(HTTPRequest request)
        {
            var address = request.GetVariable("input");
            var nftList = TokensController.GetNftListByAddress(address);
            var context = GetSessionContext(request);
            if (nftList != null && nftList.Any())
            {
                context[AppSettings.MenuContext] = _menus;
                context[AppSettings.NftTokensContext] = nftList;
                return RendererView(context, "layout", AppSettings.NftTokensContext);
            }

            _errorContextInstance.ErrorCode = "nft error";
            _errorContextInstance.ErrorDescription = $"no nfts found for this {address} address";
            context[AppSettings.ErrorContext] = _errorContextInstance;

            return HTTPResponse.Redirect(AppSettings.UrlError);
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

                context[AppSettings.MenuContext] = _menus;
                context[AppSettings.TokenContext] = token;
                context[AppSettings.HoldersContext] = holders;
                context["transfers"] = transfers;
                return RendererView(context, "layout", AppSettings.TokenContext, AppSettings.HoldersContext, "transfers");
            }
            _errorContextInstance.ErrorCode = "token error";
            _errorContextInstance.ErrorDescription = "Token not found";
            request.session.SetStruct<ErrorContext>(AppSettings.ErrorContext, _errorContextInstance);

            return HTTPResponse.Redirect(AppSettings.UrlError);
        }

        private object RouteTransactions(HTTPRequest request)
        {
            var txList = TransactionsController.GetLastTransactions();
            var context = GetSessionContext(request);
            if (txList.Count > 0)
            {
                ActivateMenuItem(AppSettings.UrlTransactions);

                context[AppSettings.MenuContext] = _menus;
                context[AppSettings.TxsContext] = txList;
                return RendererView(context, "layout", AppSettings.TxsContext);
            }

            _errorContextInstance.ErrorCode = "txs error";
            _errorContextInstance.ErrorDescription = "No transactions found";
            request.session.SetStruct<ErrorContext>(AppSettings.ErrorContext, _errorContextInstance);

            return HTTPResponse.Redirect(AppSettings.UrlError);
        }

        private object RouteTransaction(HTTPRequest request)
        {
            var txHash = request.GetVariable("input");
            var tx = TransactionsController.GetTransaction(txHash);
            var context = GetSessionContext(request);
            if (tx != null)
            {
                ActivateMenuItem(AppSettings.UrlTransaction);

                context[AppSettings.MenuContext] = _menus;
                context[AppSettings.TxContext] = tx;

                return RendererView(context, "layout", AppSettings.TxContext);
            }

            _errorContextInstance.ErrorCode = "txs error";
            _errorContextInstance.ErrorDescription = $"Transaction {txHash} not found";
            request.session.SetStruct<ErrorContext>(AppSettings.ErrorContext, _errorContextInstance);

            return HTTPResponse.Redirect(AppSettings.UrlError);
        }

        private object RouteAddresses(HTTPRequest request)
        {
            var addressList = AddressesController.GetAddressList();
            var context = GetSessionContext(request);
            if (addressList != null && addressList.Any())
            {
                ActivateMenuItem(AppSettings.UrlAddresses);
                context[AppSettings.MenuContext] = _menus;
                context[AppSettings.AddressesContext] = addressList;

                return RendererView(context, "layout", AppSettings.AddressesContext);
            }

            _errorContextInstance.ErrorCode = "Address error";
            _errorContextInstance.ErrorDescription = $"No addresses";
            request.session.SetStruct<ErrorContext>(AppSettings.ErrorContext, _errorContextInstance);

            return HTTPResponse.Redirect(AppSettings.UrlError);
        }

        private object RouteAddress(HTTPRequest request)
        {
            var addressText = request.GetVariable("input");
            var address = AddressesController.GetAddress(addressText);
            var context = GetSessionContext(request);
            if (address != null)
            {
                context[AppSettings.MenuContext] = _menus;
                context[AppSettings.AddressContext] = address;
                return RendererView(context, "layout", AppSettings.AddressContext);
            }

            _errorContextInstance.ErrorCode = "Address error";
            _errorContextInstance.ErrorDescription = $"Invalid address";
            request.session.SetStruct<ErrorContext>(AppSettings.ErrorContext, _errorContextInstance);

            return HTTPResponse.Redirect(AppSettings.UrlError);
        }

        private object RouteBlocks(HTTPRequest request)
        {
            var input = request.GetVariable("p"); //todo ask this
            if (!int.TryParse(input, out int pageNumber))
            {
                pageNumber = 1;
            }

            var pageModel = new PaginationModel
            {
                Count = BlocksController.GetBlocksCount(),
                CurrentPage = pageNumber,
                PageSize = 20,
            };

            var blocksList = BlocksController.GetBlocks(pageNumber);
            var context = GetSessionContext(request);

            if (blocksList.Count > 0)
            {
                ActivateMenuItem(AppSettings.UrlBlocks);
                context[AppSettings.MenuContext] = _menus;
                context[AppSettings.BlocksContext] = blocksList;
                context[AppSettings.PaginationContext] = pageModel;
                return RendererView(context, "layout", AppSettings.BlocksContext);
            }

            _errorContextInstance.ErrorCode = "blocks error";
            _errorContextInstance.ErrorDescription = "No blocks found";
            request.session.SetStruct<ErrorContext>(AppSettings.ErrorContext, _errorContextInstance);

            return HTTPResponse.Redirect(AppSettings.UrlError);
        }

        private object RouteBlock(HTTPRequest request)
        {
            var input = request.GetVariable("input");
            var block = BlocksController.GetBlock(input);
            var context = GetSessionContext(request);
            if (block != null)
            {
                context[AppSettings.MenuContext] = _menus;
                context[AppSettings.BlockContext] = block;
                return RendererView(context, "layout", AppSettings.BlockContext);
            }

            _errorContextInstance.ErrorCode = "blocks error";
            _errorContextInstance.ErrorDescription = $"No block found with this {input} input";
            request.session.SetStruct<ErrorContext>(AppSettings.ErrorContext, _errorContextInstance);

            return HTTPResponse.Redirect(AppSettings.UrlError);
        }

        private object RouteChains(HTTPRequest request)
        {
            var chainList = ChainsController.GetChains();
            var context = GetSessionContext(request);
            if (chainList.Count > 0)
            {
                ActivateMenuItem(AppSettings.UrlChains);
                context[AppSettings.MenuContext] = _menus;
                context[AppSettings.ChainsContext] = chainList;

                return RendererView(context, "layout", AppSettings.ChainsContext);
            }
            _errorContextInstance.ErrorCode = "chains error";
            _errorContextInstance.ErrorDescription = "No chains found";
            request.session.SetStruct<ErrorContext>(AppSettings.ErrorContext, _errorContextInstance);

            return HTTPResponse.Redirect(AppSettings.UrlError);
        }

        private object RouteChain(HTTPRequest request)
        {
            var addressText = request.GetVariable("input");
            var chain = ChainsController.GetChain(addressText);
            var context = GetSessionContext(request);
            if (chain != null)
            {
                context[AppSettings.MenuContext] = _menus;
                context[AppSettings.ChainContext] = chain;

                return RendererView(context, "layout", AppSettings.ChainContext);
            }

            _errorContextInstance.ErrorCode = "chains error";
            _errorContextInstance.ErrorDescription = $"No chain found with this {addressText} address";
            request.session.SetStruct<ErrorContext>(AppSettings.ErrorContext, _errorContextInstance);

            return HTTPResponse.Redirect(AppSettings.UrlError);
        }

        private object RouteApps(HTTPRequest request)
        {
            var appList = AppsController.GetAllApps();
            var context = GetSessionContext(request);
            if (appList.Count > 0)
            {
                context[AppSettings.MenuContext] = _menus;
                context[AppSettings.AppsContext] = appList;
                return RendererView(context, "layout", AppSettings.AppsContext);
            }
            _errorContextInstance.ErrorCode = "apps error";
            _errorContextInstance.ErrorDescription = "No apps found";
            request.session.SetStruct<ErrorContext>(AppSettings.ErrorContext, _errorContextInstance);

            return HTTPResponse.Redirect(AppSettings.UrlError);
        }
        private object RouteApp(HTTPRequest request)
        {
            var appId = request.GetVariable("input");
            var app = AppsController.GetApp(appId);
            var context = GetSessionContext(request);
            if (app != null)
            {
                context[AppSettings.MenuContext] = _menus;
                context[AppSettings.AppContext] = app;

                return RendererView(context, "layout", AppSettings.AppContext);
            }

            _errorContextInstance.ErrorCode = "apps error";
            _errorContextInstance.ErrorDescription = $"No app with {appId} found";
            request.session.SetStruct<ErrorContext>(AppSettings.ErrorContext, _errorContextInstance);

            return HTTPResponse.Redirect(AppSettings.UrlError);
        }
        #endregion

        #region API
        private void SetupAPIHandlers()
        {
            TemplateEngine.Server.Get($"{AppSettings.UrlApi}/get_account/{{address}}", request =>
            {
                var address = request.GetVariable("address");
                return ApiController.GetAccount(address);
            });

            TemplateEngine.Server.Get($"{AppSettings.UrlApi}/get_account_txs/{{address}}", request =>
            {
                var address = request.GetVariable("address");
                return ApiController.GetAddressTransactions(address);
            });

            TemplateEngine.Server.Get($"{AppSettings.UrlApi}/get_apps", request => ApiController.GetApps());

            TemplateEngine.Server.Get($"{AppSettings.UrlApi}/get_block_height/{{chain}}", request =>
            {
                var chain = request.GetVariable("chain");
                return ApiController.GetBlockHeight(chain);
            });

            TemplateEngine.Server.Get($"{AppSettings.UrlApi}/get_block/{{blockHash}}", request =>
            {
                var address = request.GetVariable("blockHash");
                return ApiController.GetBlockByHash(address);
            });

            TemplateEngine.Server.Get($"{AppSettings.UrlApi}/get_block/{{chain}}/{{height}}", request =>
            {
                var chain = request.GetVariable("chain");
                var height = (uint.Parse(request.GetVariable("height")));
                return ApiController.GetBlockByHeight(chain, height);
            });

            TemplateEngine.Server.Get($"{AppSettings.UrlApi}/get_block_tx_count_by_hash/{{blockHash}}", request =>
            {
                var block = request.GetVariable("blockHash");
                return ApiController.GetBlockTransactionCountByHash(block);
            });

            TemplateEngine.Server.Get($"{AppSettings.UrlApi}/get_confirmations/{{txHash}}", request =>
            {
                var txHash = request.GetVariable("txHash");
                return ApiController.GetConfirmations(txHash);
            });

            TemplateEngine.Server.Get($"{AppSettings.UrlApi}/get_tx_by_block_hash_index/{{block}}/{{index}}", request =>
            {
                var block = request.GetVariable("block");
                var index = int.Parse(request.GetVariable("index"));
                return ApiController.GetTransactionByBlockHashAndIndex(block, index);
            });

            TemplateEngine.Server.Get($"{AppSettings.UrlApi}/get_chains", request => ApiController.GetChains());

            TemplateEngine.Server.Get($"{AppSettings.UrlApi}/get_tokens", request => ApiController.GetTokens());

            //todo confirm this
            TemplateEngine.Server.Post($"{AppSettings.UrlApi}/send_raw_tx/{{signedTx}}", request =>
            {
                var signedTx = request.GetVariable("signedTx");
                return ApiController.SendRawTransaction(signedTx);
            });
        }
        #endregion

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