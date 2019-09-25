using System;
using System.Collections.Generic;
using System.Linq;
using LunarLabs.WebServer.HTTP;
using LunarLabs.WebServer.Templates;
using Phantasma.Core;
using Phantasma.Explorer.Application;
using Phantasma.Explorer.Controllers;
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
        private Dictionary<string, object> GetSessionContext(HTTPRequest request) => request.session.Data.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        public void Init()
        {
            _menus = new List<MenuContext>
            {
                new MenuContext {Text = "Transactions", Url = AppSettings.UrlTransactions, Active = true},
                new MenuContext {Text = "Chains", Url = AppSettings.UrlChains, Active = false},
                new MenuContext {Text = "Blocks", Url = AppSettings.UrlBlocks, Active = false},
                new MenuContext {Text = "Tokens", Url = AppSettings.UrlTokens, Active = false},
                new MenuContext {Text = "Accounts", Url = AppSettings.UrlAddresses, Active = false},
                //new MenuContext {Text = "Apps", Url = AppSettings.UrlApps, Active = false},
                new MenuContext {Text = "Soul Masters", Url = AppSettings.UrlSoulMasters, Active = false},
                new MenuContext {Text = "Marketplace", Url = AppSettings.UrlMarketplace, Active = false}
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
                var marketCap = CoinUtils.GetCoinMarketCap().GetAwaiter().GetResult();
                return $"$ {marketCap}";
            });

            TemplateEngine.Server.Get("/rates", request =>
            {
                //var coins = HomeController.GetRateInfo();

                var html = TemplateEngine.Render("", "rates");
                return html;
            });

            TemplateEngine.Server.Get($"{AppSettings.UrlToken}/{{input}}", RouteToken);

            TemplateEngine.Server.Get($"{AppSettings.UrlTransactions}", RouteTransactions);
            TemplateEngine.Server.Get($"{AppSettings.UrlTransactions}/{{page}}", RouteTransactions);

            TemplateEngine.Server.Get($"{AppSettings.UrlTransaction}/{{input}}", RouteTransaction);

            TemplateEngine.Server.Get($"{AppSettings.UrlAddresses}", RouteAddresses);

            TemplateEngine.Server.Get($"{AppSettings.UrlAddress}/{{input}}", RouteAddress);
            TemplateEngine.Server.Get($"{AppSettings.UrlAddress}/{{input}}/{{page}}", RouteAddress);

            TemplateEngine.Server.Get($"{AppSettings.UrlBlocks}", RouteBlocks);
            TemplateEngine.Server.Get($"{AppSettings.UrlBlocks}/{{page}}", RouteBlocks);

            TemplateEngine.Server.Get($"{AppSettings.UrlBlock}/{{input}}", RouteBlock);

            TemplateEngine.Server.Get($"{AppSettings.UrlChains}", RouteChains);

            TemplateEngine.Server.Get($"{AppSettings.UrlChain}/{{input}}", RouteChain);

            TemplateEngine.Server.Get($"{AppSettings.UrlApps}", RouteApps);

            TemplateEngine.Server.Get($"{AppSettings.UrlSoulMasters}", RouteSoulMasters);
            TemplateEngine.Server.Get($"{AppSettings.UrlSoulMasters}/{{page}}", RouteSoulMasters);


            TemplateEngine.Server.Get($"{AppSettings.UrlApp}/{{input}}", RouteApp);

            TemplateEngine.Server.Get($"{AppSettings.UrlMarketplace}", RouteMarketplace);
            TemplateEngine.Server.Get($"{AppSettings.UrlMarketplace}/{{chain}}/{{page}}", RouteMarketplace);

            //SetupAPIHandlers(); todo
        }

        #region ROUTES

        private object RouteHome(HTTPRequest request)
        {
            var context = GetSessionContext(request);
            var blocksAndTxs = HomeControllerInstance.GetLastestInfo();
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
                    var url = HomeControllerInstance.SearchCommand(searchInput);
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
            try
            {
                var tokensList = TokensControllerInstance.GetTokens();
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
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            _errorContextInstance.ErrorCode = "token error";
            _errorContextInstance.ErrorDescription = "Tokens not found";
            request.session.SetStruct<ErrorContext>(AppSettings.ErrorContext, _errorContextInstance);

            return HTTPResponse.Redirect(AppSettings.UrlError);
        }

        private object RouteTokensNft(HTTPRequest request)
        {
            var address = request.GetVariable("input");
            try
            {
                var nftList = TokensControllerInstance.GetNftListByAddress(address);
                var context = GetSessionContext(request);
                if (nftList != null && nftList.Any())
                {
                    context[AppSettings.MenuContext] = _menus;
                    context[AppSettings.NftTokensContext] = nftList;
                    return RendererView(context, "layout", AppSettings.NftTokensContext);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            _errorContextInstance.ErrorCode = "nft error";
            _errorContextInstance.ErrorDescription = $"no nfts found for this {address} address";
            request.session.SetStruct<ErrorContext>(AppSettings.ErrorContext, _errorContextInstance);

            return HTTPResponse.Redirect(AppSettings.UrlError);
        }

        private object RouteToken(HTTPRequest request)
        {
            try
            {
                var tokenSymbol = request.GetVariable("input");
                var controller = TokensControllerInstance;
                var token = controller.GetToken(tokenSymbol);
                var context = GetSessionContext(request);
                if (token != null)
                {
                    var holders = controller.GetHolders(token.Symbol);
                    var transfers = controller.GetTransfers(token.Symbol);

                    context[AppSettings.MenuContext] = _menus;
                    context[AppSettings.TokenContext] = token;
                    context[AppSettings.HoldersContext] = holders;
                    context[AppSettings.TokenTransfersContext] = transfers;
                    return RendererView(context, "layout", AppSettings.TokenContext, AppSettings.HoldersContext, "transfers");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            _errorContextInstance.ErrorCode = "token error";
            _errorContextInstance.ErrorDescription = "Token not found";
            request.session.SetStruct<ErrorContext>(AppSettings.ErrorContext, _errorContextInstance);

            return HTTPResponse.Redirect(AppSettings.UrlError);
        }

        private object RouteTransactions(HTTPRequest request)
        {
            try
            {
                var input = request.GetVariable("page");
                if (!int.TryParse(input, out int pageNumber))
                {
                    pageNumber = 1;
                }

                var controller = TransactionsControllerInstance;
                var pageModel = new PaginationModel
                {
                    Count = controller.GetTransactionsCount(),
                    CurrentPage = pageNumber,
                    PageSize = AppSettings.PageSize,
                };

                var txList = controller.GetTransactions(pageModel.CurrentPage, pageModel.PageSize);
                var context = GetSessionContext(request);

                if (txList?.Count > 0)
                {
                    ActivateMenuItem(AppSettings.UrlTransactions);

                    context[AppSettings.MenuContext] = _menus;
                    context[AppSettings.TxsContext] = txList;
                    context[AppSettings.PaginationContext] = pageModel;
                    return RendererView(context, "layout", AppSettings.TxsContext);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            _errorContextInstance.ErrorCode = "txs error";
            _errorContextInstance.ErrorDescription = "No transactions found";
            request.session.SetStruct<ErrorContext>(AppSettings.ErrorContext, _errorContextInstance);

            return HTTPResponse.Redirect(AppSettings.UrlError);
        }

        private object RouteTransaction(HTTPRequest request)
        {
            var txHash = request.GetVariable("input");
            try
            {
                var tx = TransactionsControllerInstance.GetTransaction(txHash);
                var context = GetSessionContext(request);
                if (tx != null)
                {
                    ActivateMenuItem(AppSettings.UrlTransaction);

                    context[AppSettings.MenuContext] = _menus;
                    context[AppSettings.TxContext] = tx;

                    return RendererView(context, "layout", AppSettings.TxContext);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            _errorContextInstance.ErrorCode = "txs error";
            _errorContextInstance.ErrorDescription = $"Transaction {txHash} not found";
            request.session.SetStruct<ErrorContext>(AppSettings.ErrorContext, _errorContextInstance);

            return HTTPResponse.Redirect(AppSettings.UrlError);
        }

        private object RouteAddresses(HTTPRequest request)
        {
            try
            {
                var addressList = AddressesControllerInstance.GetAddressList();

                var context = GetSessionContext(request);
                if (addressList != null && addressList.Any())
                {
                    ActivateMenuItem(AppSettings.UrlAddresses);
                    context[AppSettings.MenuContext] = _menus;
                    context[AppSettings.AddressesContext] = addressList;

                    return RendererView(context, "layout", AppSettings.AddressesContext);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            _errorContextInstance.ErrorCode = "Address error";
            _errorContextInstance.ErrorDescription = $"No addresses";
            request.session.SetStruct<ErrorContext>(AppSettings.ErrorContext, _errorContextInstance);

            return HTTPResponse.Redirect(AppSettings.UrlError);
        }

        private object RouteAddress(HTTPRequest request)
        {
            try
            {
                var addressText = request.GetVariable("input");
                var context = GetSessionContext(request);
                if (AddressesControllerInstance.IsAddressStored(addressText))
                {
                    var input = request.GetVariable("page");
                    if (!int.TryParse(input, out int pageNumber))
                    {
                        pageNumber = 1;
                    }
                    var controller = AddressesControllerInstance;

                    var pageModel = new PaginationModel
                    {
                        Count = controller.GetTransactionCount(addressText),
                        CurrentPage = pageNumber,
                        PageSize = AppSettings.PageSize,
                    };
                    var addressVm = AddressesControllerInstance.GetAddress(addressText, pageModel.CurrentPage, pageModel.PageSize);
                    context[AppSettings.MenuContext] = _menus;
                    context[AppSettings.AddressContext] = addressVm;
                    context[AppSettings.PaginationContext] = pageModel;
                    return RendererView(context, "layout", AppSettings.AddressContext);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            _errorContextInstance.ErrorCode = "Address error";
            _errorContextInstance.ErrorDescription = $"Invalid address";
            request.session.SetStruct<ErrorContext>(AppSettings.ErrorContext, _errorContextInstance);

            return HTTPResponse.Redirect(AppSettings.UrlError);
        }

        private object RouteBlocks(HTTPRequest request)
        {
            try
            {
                var input = request.GetVariable("page");
                if (!int.TryParse(input, out int pageNumber))
                {
                    pageNumber = 1;
                }

                var controller = BlocksControllerInstance;
                var pageModel = new PaginationModel
                {
                    Count = controller.GetBlocksCount(),
                    CurrentPage = pageNumber,
                    PageSize = AppSettings.PageSize,
                };

                var blocksList = controller.GetBlocks(pageModel.CurrentPage, pageModel.PageSize);
                var context = GetSessionContext(request);

                if (blocksList.Count > 0)
                {
                    ActivateMenuItem(AppSettings.UrlBlocks);
                    context[AppSettings.MenuContext] = _menus;
                    context[AppSettings.BlocksContext] = blocksList;
                    context[AppSettings.PaginationContext] = pageModel;
                    return RendererView(context, "layout", AppSettings.BlocksContext);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            _errorContextInstance.ErrorCode = "blocks error";
            _errorContextInstance.ErrorDescription = "No blocks found";
            request.session.SetStruct<ErrorContext>(AppSettings.ErrorContext, _errorContextInstance);

            return HTTPResponse.Redirect(AppSettings.UrlError);
        }

        private object RouteBlock(HTTPRequest request)
        {
            var input = request.GetVariable("input");
            try
            {
                var block = BlocksControllerInstance.GetBlock(input);
                var context = GetSessionContext(request);
                if (block != null)
                {
                    context[AppSettings.MenuContext] = _menus;
                    context[AppSettings.BlockContext] = block;
                    return RendererView(context, "layout", AppSettings.BlockContext);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            _errorContextInstance.ErrorCode = "blocks error";
            _errorContextInstance.ErrorDescription = $"No block found with this {input} input";
            request.session.SetStruct<ErrorContext>(AppSettings.ErrorContext, _errorContextInstance);

            return HTTPResponse.Redirect(AppSettings.UrlError);
        }

        private object RouteChains(HTTPRequest request)
        {
            try
            {
                var chainList = ChainsControllerInstance.GetChains();
                var context = GetSessionContext(request);
                if (chainList.Count > 0)
                {
                    ActivateMenuItem(AppSettings.UrlChains);
                    context[AppSettings.MenuContext] = _menus;
                    context[AppSettings.ChainsContext] = chainList;

                    return RendererView(context, "layout", AppSettings.ChainsContext);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            _errorContextInstance.ErrorCode = "chains error";
            _errorContextInstance.ErrorDescription = "No chains found";
            request.session.SetStruct<ErrorContext>(AppSettings.ErrorContext, _errorContextInstance);

            return HTTPResponse.Redirect(AppSettings.UrlError);
        }

        private object RouteChain(HTTPRequest request)
        {
            var addressText = request.GetVariable("input");
            try
            {
                var chain = ChainsControllerInstance.GetChain(addressText);
                var context = GetSessionContext(request);
                if (chain != null)
                {
                    context[AppSettings.MenuContext] = _menus;
                    context[AppSettings.ChainContext] = chain;

                    return RendererView(context, "layout", AppSettings.ChainContext);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            _errorContextInstance.ErrorCode = "chains error";
            _errorContextInstance.ErrorDescription = $"No chain found with this {addressText} address";
            request.session.SetStruct<ErrorContext>(AppSettings.ErrorContext, _errorContextInstance);

            return HTTPResponse.Redirect(AppSettings.UrlError);
        }

        private object RouteApps(HTTPRequest request)
        {
            try
            {
                var controller = AppsControllerInstance;
                var appList = controller.GetAllApps();
                var context = GetSessionContext(request);
                if (appList.Count > 0)
                {
                    context[AppSettings.MenuContext] = _menus;
                    context[AppSettings.AppsContext] = appList;
                    return RendererView(context, "layout", AppSettings.AppsContext);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            _errorContextInstance.ErrorCode = "apps error";
            _errorContextInstance.ErrorDescription = "No apps found";
            request.session.SetStruct<ErrorContext>(AppSettings.ErrorContext, _errorContextInstance);

            return HTTPResponse.Redirect(AppSettings.UrlError);
        }

        private object RouteSoulMasters(HTTPRequest request)
        {
            try
            {
                var input = request.GetVariable("page");
                var controller = SoulMastersControllerInstance;

                if (!int.TryParse(input, out int pageNumber))
                {
                    pageNumber = 1;
                }

                var pageModel = new PaginationModel
                {
                    Count = controller.GetSoulsMasterCount(),
                    CurrentPage = pageNumber,
                    PageSize = AppSettings.PageSize,
                };

                var soulMasterList = controller.GetSoulMasters(pageModel.CurrentPage, pageModel.PageSize);
                var context = GetSessionContext(request);
                if (soulMasterList.Count > 0)
                {
                    context[AppSettings.MenuContext] = _menus;
                    context[AppSettings.SoulMastersContext] = soulMasterList;
                    return RendererView(context, "layout", AppSettings.SoulMastersContext);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            _errorContextInstance.ErrorCode = "apps error";
            _errorContextInstance.ErrorDescription = "No Soul Masters found";
            request.session.SetStruct<ErrorContext>(AppSettings.ErrorContext, _errorContextInstance);

            return HTTPResponse.Redirect(AppSettings.UrlError);
        }

        private object RouteApp(HTTPRequest request)
        {
            var appId = request.GetVariable("input");
            try
            {
                var controller = AppsControllerInstance;
                var app = controller.GetApp(appId);
                var context = GetSessionContext(request);
                if (app != null)
                {
                    context[AppSettings.MenuContext] = _menus;
                    context[AppSettings.AppContext] = app;

                    return RendererView(context, "layout", AppSettings.AppContext);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            _errorContextInstance.ErrorCode = "apps error";
            _errorContextInstance.ErrorDescription = $"No app with {appId} found";
            request.session.SetStruct<ErrorContext>(AppSettings.ErrorContext, _errorContextInstance);

            return HTTPResponse.Redirect(AppSettings.UrlError);
        }

        private object RouteMarketplace(HTTPRequest request)
        {
            try
            {
                var input = request.GetVariable("page");
                if (!int.TryParse(input, out int pageNumber))
                {
                    pageNumber = 1;
                }

                var selectedChain = request.GetVariable("chain");


                var controller = MarketplaceControllerInstance;
                var chainsWithMarkets = controller.GetChainsWithMarketsAndActiveAuctions().Result;

                if (string.IsNullOrEmpty(selectedChain) && chainsWithMarkets.Any())
                {
                    selectedChain = chainsWithMarkets[0]; //default
                }

                var pageModel = new PaginationModel
                {
                    Count = controller.GetAuctionsCount(selectedChain).Result,
                    CurrentPage = pageNumber,
                    PageSize = AppSettings.PageSize,
                };

                var marketVm = controller.GetAuctions(selectedChain, pageModel.CurrentPage, pageModel.PageSize).Result;
                var context = GetSessionContext(request);

                if (marketVm.TotalAuctions >= 0)
                {
                    context[AppSettings.MenuContext] = _menus;
                    context[AppSettings.MarketplaceContext] = marketVm;
                    context["marketChains"] = chainsWithMarkets;
                    context["selectedChain"] = selectedChain;
                    context[AppSettings.PaginationContext] = pageModel;
                    return RendererView(context, "layout", AppSettings.MarketplaceContext);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            _errorContextInstance.ErrorCode = "Marketplace error";
            _errorContextInstance.ErrorDescription = "No active auctions found";
            request.session.SetStruct<ErrorContext>(AppSettings.ErrorContext, _errorContextInstance);

            return HTTPResponse.Redirect(AppSettings.UrlError);
        }
        #endregion


        public class MenuContext
        {
            public string Text { get; set; }
            public string Url { get; set; }
            public bool Active { get; set; }
        }

        public struct ErrorContext
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
        private HomeController HomeControllerInstance => new HomeController();
        private AddressesController AddressesControllerInstance => new AddressesController();
        private BlocksController BlocksControllerInstance => new BlocksController();
        private ChainsController ChainsControllerInstance => new ChainsController();
        private TransactionsController TransactionsControllerInstance => new TransactionsController();
        private TokensController TokensControllerInstance => new TokensController();
        private AppsController AppsControllerInstance => new AppsController();
        private SoulMastersController SoulMastersControllerInstance => new SoulMastersController();
        private MarketplaceController MarketplaceControllerInstance => new MarketplaceController();
        //private ApiController ApiControllerInstance => new ApiController();
        #endregion
    }
}