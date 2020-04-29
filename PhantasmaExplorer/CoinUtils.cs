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
                  baseticker = "phantasma";
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

          var url = $"https://api.coingecko.com/api/v3/simple/price?ids={baseticker}&vs_currencies={currrency}";

          try
          {
              using (var httpClient = new HttpClient())
              {
                json = httpClient.GetStringAsync(new Uri(url)).Result;
              }
              var root = JSONReader.ReadFromString(json);

              // hack for kcal price 1/5 soul & goati .10
              if (ticker == "KCAL")
              {
                root = root["phantasma"];
                var price = root.GetDecimal(currrency.ToLower())/5;
                return price;
              }
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