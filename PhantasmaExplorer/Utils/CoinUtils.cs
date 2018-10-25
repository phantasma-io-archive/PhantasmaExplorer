using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using LunarLabs.Parser.JSON;

namespace Phantasma.Explorer.Utils
{
    public static class CoinUtils
    {
        public static decimal GetUsdCoinRate(uint ticker)
        {
            var url = $"https://api.coinmarketcap.com/v2/ticker/{ticker}/";

            string json;

            try
            {
                using (var wc = new WebClient())
                {
                    json = wc.DownloadString(url);
                }

                var root = JSONReader.ReadFromString(json);

                root = root["data"];

                var quotes = root["quotes"]["USD"];

                var price = quotes.GetDecimal("price");

                return price;
            }
            catch
            {
                return 0;
            }
        }


        public static async Task<decimal> GetCoinRate(uint ticker, string coin)
        {
            var url = $"https://api.coinmarketcap.com/v2/ticker/{ticker}/?convert={coin}";
            
            try
            {
                var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(new Uri(url));
                var root = JSONReader.ReadFromString(await response.Content.ReadAsStringAsync());

                root = root["data"];
                var quotes = root["quotes"][coin];

                var price = quotes.GetDecimal("price");

                return price;
            }
            catch
            {
                return 0;
            }
        }

        public static async Task<decimal> GetCoinMarketCap(uint ticker)
        {
            var url = $"https://api.coinmarketcap.com/v2/ticker/{ticker}/?convert=USD";

            try
            {
                var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(new Uri(url));
                var root = JSONReader.ReadFromString(await response.Content.ReadAsStringAsync());

                root = root["data"];
                var quotes = root["quotes"]["USD"];

                var marketCap = quotes.GetDecimal("market_cap");

                return marketCap;
            }
            catch
            {
                return 0;
            }
        }
    }
}