using LunarLabs.Parser.JSON;

namespace Phantasma.Explorer
{
    public static class CoinUtils
    {
        public static decimal GetCoinRate(uint ticker)
        {
            var url = $"https://api.coinmarketcap.com/v2/ticker/{ticker}/";

            string json;

            try
            {
                using (var wc = new System.Net.WebClient())
                {
                    json = wc.DownloadString(url);
                }

                var root = JSONReader.ReadFromString(json);

                root = root["data"];

                var rank = root.GetInt32("rank");

                var quotes = root["quotes"]["USD"];

                var price = quotes.GetDecimal("price");

                var mcap = quotes.GetDecimal("market_cap");

                return price;
            }
            catch
            {
                return 0;
            }
        }
    }
}
