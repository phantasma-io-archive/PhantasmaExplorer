using Phantasma.Blockchain.Contracts.Native;

namespace Phantasma.Explorer.ViewModels
{
    public class AppViewModel
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public int TxCount { get; set; }
        public int Rank { get; set; }

        public static AppViewModel FromApp(AppInfo info)
        {
            return new AppViewModel
            {
                Id = info.id,
                Title = info.title,
                //Url = info.url,
                Url = "https://phantasma.io",
                Description = info.description,
                //Icon = info.icon?.ToString()
                Icon = Explorer.MockLogoUrl
            };
        }
    }
}
