using System.Net;
using System.Threading.Tasks;
using LunarLabs.Parser;
using LunarLabs.Parser.JSON;

namespace Phantasma.Explorer.Utils
{
    public static class CoinUtils
    {
        public static async Task<decimal> GetCoinRateAsync(uint ticker, string symbol = "USD")
        {
            return await Task.FromResult(GetCoinRate(ticker, symbol));
        }

        public static async Task<DataNode> GetCoinInfoAsync(uint ticker, string symbol = "USD")
        {
            return await Task.FromResult(GetCoinInfo(ticker, symbol));
        }

        public static decimal GetCoinRate(uint ticker, string symbol = "USD")
        {
            var url = $"https://api.coinmarketcap.com/v2/ticker/{ticker}/?convert={symbol}";

            string json;

            try
            {
                using (var wc = new WebClient())
                {
                    json = wc.DownloadString(url);
                }

                var root = JSONReader.ReadFromString(json);

                root = root["data"];
                var quotes = root["quotes"][symbol];

                var price = quotes.GetDecimal("price");

                return price;
            }
            catch
            {
                return 0;
            }
        }

        public static DataNode GetCoinInfo(uint ticker, string symbol = "USD")
        {
            var url = $"https://api.coinmarketcap.com/v2/ticker/{ticker}/?convert={symbol}";
            string json;

            try
            {
                using (var wc = new WebClient())
                {
                    json = wc.DownloadString(url);
                }

                var root = JSONReader.ReadFromString(json);

                root = root["data"];
                return root;
            }
            catch
            {
                return null;
            }
        }
    }
}