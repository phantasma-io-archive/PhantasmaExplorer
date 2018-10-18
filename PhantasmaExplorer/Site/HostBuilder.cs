using LunarLabs.WebServer.Core;
using LunarLabs.WebServer.HTTP;

namespace Phantasma.Explorer.Site
{
    public static class HostBuilder
    {
        public static LunarLabs.WebServer.Core.Site CreateSite(string[] args, string filePath)
        {
            var log = new ConsoleLogger();

            // either parse the settings from the program args or initialize them manually
            var settings = ServerSettings.Parse(args);

            var server = new HTTPServer(log, settings);

            // instantiate a new site, the second argument is the relative file path where the public site contents will be found
            return new LunarLabs.WebServer.Core.Site(server, filePath);
        }
    }
}
