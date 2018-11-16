using System;
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
            //todo remove
            Random rnd = new Random();

            List<AppViewModel> appsList = new List<AppViewModel>();
            var apps = Repository.GetApps();
            foreach (var appInfo in apps)
            {
                var chain = Repository.GetChainByName(appInfo.id);
                var txs = Repository.GetAddressTransactions(chain.Address).ToList();
                var vm = AppViewModel.FromApp(appInfo);
                vm.TxCount = txs.Count;
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
            var app = apps.SingleOrDefault(a => a.id == appId);
            return AppViewModel.FromApp(app);
        }
    }
}
