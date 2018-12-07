using LunarLabs.WebServer.Core;
using LunarLabs.WebServer.HTTP;

namespace Phantasma.Explorer.Site
{
    public static class HostBuilder
    {
        public static HTTPServer CreateServer(string[] args)
        {
            var log = new ConsoleLogger();

            // either parse the settings from the program args or initialize them manually
            var settings = ServerSettings.Parse(args);
            settings.Compression = false;

            return new HTTPServer(settings, log);
        }
    }
}
