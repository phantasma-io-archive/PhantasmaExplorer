using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using LunarLabs.Parser;
using LunarLabs.Parser.JSON;
using Newtonsoft.Json.Linq;

namespace Phantasma.Explorer.Utils
{
    public static class CoinUtils
    {
        public const int SoulId = 2827;

        //public static async Task<DataNode> GetCoinInfoAsync(uint ticker, string symbol = "USD")
        //{
        //    return await Task.FromResult(GetCoinInfo(ticker, symbol));
        //}

        public static async Task<DataNode> GetCoinInfo(string coin = "phantasma", string currency = "usd")
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("https://api.coingecko.com/api/v3/");
            var response = await client.GetAsync($"simple/price?ids={coin}&vs_currencies={currency}&include_market_cap=true");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var jsonObject = JSONReader.ReadFromString(content);
                var phantasma = jsonObject["phantasma"];
                return phantasma;
            }

            return null;
        }

        public static async Task<string> GetCoinMarketCap(string coin = "phantasma", string currency = "usd")
        {
            var info = await GetCoinInfo(coin, currency);
            if (info != null)
            {
                return info["usd_market_cap"].AsString();
            }
            return "";
        }

        public static async Task<string> GetCoinPrice(string coin = "phantasma", string currency = "usd")
        {
            var info = await GetCoinInfo(coin, currency);
            if (info != null)
            {
                return info[currency].AsString();
            }
            return "";
        }



        //public static DataNode GetCoinInfo(uint ticker, string symbol = "USD")
        //{
        //    var url = $"https://api.coinmarketcap.com/v2/ticker/{ticker}/?convert={symbol}";
        //    string json;

        //    try
        //    {
        //        using (var wc = new WebClient())
        //        {
        //            json = wc.DownloadString(url);
        //        }

        //        var root = JSONReader.ReadFromString(json);

        //        root = root["data"];
        //        return root;
        //    }
        //    catch(Exception ex)
        //    {
        //        return null;
        //    }
        //}

        //public static decimal[] GetChartForCoin(string symbol, string quote = "USD", int days = 30)
        //{
        //    var result = new decimal[days];

        //    var url = $"https://min-api.cryptocompare.com/data/histoday?fsym={symbol}&tsym={quote}&limit={days}";
        //    string json;

        //    try
        //    {
        //        using (var wc = new WebClient())
        //        {
        //            json = wc.DownloadString(url);
        //        }

        //        var root = JSONReader.ReadFromString(json);

        //        root = root["data"];
        //        for (int i = 0; i < days; i++)
        //        {
        //            var entry = root.GetNodeByIndex(i);
        //            var val = entry.GetDecimal("high");
        //            result[(days - 1) - i] = val;
        //        }

        //        return result;
        //    }
        //    catch
        //    {
        //        return null;
        //    }
        //}
    }
}