using System.Collections.Generic;
using System.Linq;
using Phantasma.Explorer.Application.Queries;
using Phantasma.Explorer.ViewModels;

namespace Phantasma.Explorer.Controllers
{
    public class AppsController
    {
        public List<AppViewModel> GetAllApps()
        {
            var appQuery = new AppQueries();
            var chainQuery = new ChainQueries();
            var txQuery = new TransactionQueries();
            List<AppViewModel> appsList = new List<AppViewModel>();
            var apps = appQuery.QueryApps();

            foreach (var appInfo in apps)
            {
                var chain = chainQuery.QueryChain(appInfo.Id);
                var txCount = txQuery.QueryTotalChainTransactionCount(chain.Address);
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
            var appQuery = new AppQueries();
            var app = appQuery.QueryApp(appId);
            return AppViewModel.FromApp(app);
        }
    }
}
