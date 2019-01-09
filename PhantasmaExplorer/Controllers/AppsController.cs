using System.Collections.Generic;
using System.Linq;
using Phantasma.Cryptography;
using Phantasma.Explorer.Infrastructure.Interfaces;
using Phantasma.Explorer.ViewModels;

namespace Phantasma.Explorer.Controllers
{
    public class AppsController
    {
        private IRepository Repository { get; set; }
        public AppsController(IRepository repo)
        {
            Repository = repo;
        }

        public List<AppViewModel> GetAllApps()
        {
            List<AppViewModel> appsList = new List<AppViewModel>();
            var apps = Repository.GetApps();
            foreach (var appInfo in apps)
            {
                var chain = Repository.GetChain(appInfo.Id);
                var txCount = 0;// todo Repository.GetAddressTransactionCount(Address.FromText(chain.Address));
                var vm = AppViewModel.FromApp(appInfo);
                vm.TxCount = txCount;
                appsList.Add(vm);
            }

            appsList = appsList.OrderByDescending(x => x.TxCount).ToList();
            for (int i = 0; i < appsList.Count; i++)
            {
                appsList[i].Rank = i + 1;
            }
            return appsList;
        }

        public AppViewModel GetApp(string appId)
        {
            var apps = Repository.GetApps();
            var app = apps.SingleOrDefault(a => a.Id == appId);
            return AppViewModel.FromApp(app);
        }
    }
}
