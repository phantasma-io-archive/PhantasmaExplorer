using System.Net;
using System.Threading.Tasks;
using LunarLabs.Parser;
using LunarLabs.Parser.JSON;

namespace Phantasma.Explorer.Utils
{
    public static class CoinUtils
    {
        public const int SoulId = 2827;

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

        public static decimal[] GetChartForCoin(string symbol, string quote = "USD", int days = 30)
        {
            var result = new decimal[days];

            var url = $"https://min-api.cryptocompare.com/data/histoday?fsym={symbol}&tsym={quote}&limit={days}";
            string json;

            try
            {
                using (var wc = new WebClient())
                {
                    json = wc.DownloadString(url);
                }

                var root = JSONReader.ReadFromString(json);

                root = root["data"];
                for (int i=0; i<days; i++)
                {
                    var entry = root.GetNodeByIndex(i);
                    var val = entry.GetDecimal("high");
                    result[(days-1) - i] = val;
                }

                return result;
            }
            catch
            {
                return null;
            }
        }
    }
}