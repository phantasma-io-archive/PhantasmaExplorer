using LunarLabs.WebServer.Core;
using LunarLabs.WebServer.HTTP;

namespace Phantasma.Explorer.Site
{
    public static class HostBuilder
    {
        public static HTTPServer CreateServer(string[] args)
        {
            // either parse the settings from the program args or initialize them manually
            var settings = ServerSettings.Parse(args);
            settings.Compression = false;

            return new HTTPServer(settings, null);
        }
    }
}
