using Phantasma.Blockchain;
using Phantasma.Domain;

namespace Phantasma.Explorer.Application
{
    internal static class AppSettings
    {
        internal const int PageSize = 20;
        internal const int SyncTime = 20000;
        internal const string MockLogoUrl = "https://s2.coinmarketcap.com/static/img/coins/32x32/2827.png";

#if DEBUG
        internal static string RpcServerUrl = "http://localhost:7077/rpc";
#else
        internal static string RpcServerUrl = "http://45.76.88.140:7077/rpc";
#endif
        internal static string FuelSymbol = DomainSettings.FuelTokenSymbol;
        internal static string NativeSymbol = DomainSettings.FiatTokenSymbol;
        internal static string FiatSymbol = DomainSettings.FiatTokenSymbol;

        internal static int FuelDecimals = DomainSettings.FuelTokenDecimals;
        internal static int StakingDecimals = DomainSettings.StakingTokenDecimals;
        internal static int FiatDecimals = DomainSettings.FiatTokenDecimals;

        #region URL&CONTEXT
        internal const string UrlHome = "/home";
        internal const string UrlTokens = "/tokens";
        internal const string UrlToken = "/token";
        internal const string UrlTransactions = "/transactions";
        internal const string UrlTransaction = "/tx";
        internal const string UrlChains = "/chains";
        internal const string UrlChain = "/chain";
        internal const string UrlBlocks = "/blocks";
        internal const string UrlBlock = "/block";
        internal const string UrlAddresses = "/addresses";
        internal const string UrlAddress = "/address";
        internal const string UrlApps = "/apps";
        internal const string UrlApp = "/app";
        internal const string UrlError = "/error";
        internal const string UrlApi = "/api";
        internal const string UrlMarketplace = "/marketplace";
        internal const string UrlSoulMasters = "/soulmasters";

        internal const string HomeContext = "home";
        internal const string MenuContext = "menu";
        internal const string TokensContext = "tokens";
        internal const string TokenTransfersContext = "transfers";
        internal const string NftTokensContext = "nftTokens";
        internal const string TokenContext = "token";
        internal const string TxContext = "transaction";
        internal const string TxsContext = "transactions";
        internal const string AddressesContext = "addresses";
        internal const string AddressContext = "address";
        internal const string BlocksContext = "blocks";
        internal const string BlockContext = "block";
        internal const string ChainsContext = "chains";
        internal const string ChainContext = "chain";
        internal const string AppsContext = "apps";
        internal const string AppContext = "app";
        internal const string SoulMastersContext = "soulmasters";
        internal const string ErrorContext = "error";
        internal const string HoldersContext = "holders";
        internal const string PaginationContext = "pagination";
        internal const string MarketplaceContext = "marketplace";

        #endregion
    }
}
