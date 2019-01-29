using System.Collections.Generic;

namespace Phantasma.Explorer.ViewModels
{
    public class NftViewModel
    {
        public string Symbol { get; set; }
        public string Address { get; set; }
        public List<NftInfoViewModel> InfoList { get; set; } = new List<NftInfoViewModel>();
        public bool HasViewerUrl => InfoList.Count > 0 && !string.IsNullOrEmpty(InfoList[0].ViewerUrl);


    }

    public class NftInfoViewModel
    {
        public string Info { get; set; }
        public string ViewerUrl { get; set; }
        public string Id { get; set; }
        public string ComposeUrl => ViewerUrl.Replace("$ID", Id);
    }
}
