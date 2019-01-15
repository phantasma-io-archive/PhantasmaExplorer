using Phantasma.Explorer.Domain.Entities;

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

        public static AppViewModel FromApp(App info)
        {
            return new AppViewModel
            {
                Id = info.Id,
                Title = info.Title,
                Url = info.Url,
                Description = info.Description,
               // Icon = info.Icon,
                Icon = Explorer.MockLogoUrl
            };
        }
    }
}
