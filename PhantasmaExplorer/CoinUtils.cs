using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using LunarLabs.Parser;
using LunarLabs.Parser.JSON;

namespace Phantasma.Explorer.Utils
{
    public static class CoinUtils
    {

      public static decimal GetCoinRate(string ticker, string currrency)
      {
          string json;
          string baseticker;
          switch (ticker)
          {
              case "SOUL":
                  baseticker = "phantasma";
                  break;
              case "KCAL":
                  baseticker = "phantasma-energy";
                  break;
              case "NEO":
                  baseticker = "neo";
                  break;
              case "GAS":
                  baseticker = "gas";
                  break;
              case "USDT":
                  baseticker = "tether";
                  break;
              case "ETH":
                  baseticker = "ethereum";
                  break;
              case "DAI":
                  baseticker = "dai";
                  break;
              default:
                  baseticker = "";
                  break;
          }

          if (String.IsNullOrEmpty(baseticker))
              return 0;

          var url = $"https://api.coingecko.com/api/v3/simple/price?ids={baseticker}&vs_currencies={currrency}";

          try
          {
              using (var httpClient = new HttpClient())
              {
                json = httpClient.GetStringAsync(new Uri(url)).Result;
              }
              var root = JSONReader.ReadFromString(json);

              // hack for goati price .10
              else if (ticker == "GOATI") {
                var price = 0.10m;
                return price;
              }
              else {
                root = root[baseticker];
                var price = root.GetDecimal(currrency.ToLower());
                return price;
              }

          }
          catch (Exception ex)
          {
              Console.WriteLine($"Exception occurred: {ex}");
              return 0;
          }
      }

    }
}