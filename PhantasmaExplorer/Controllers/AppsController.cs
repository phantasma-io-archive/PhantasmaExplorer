using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Phantasma.Explorer.Application.Queries;
using Phantasma.Explorer.Persistance;
using Phantasma.Explorer.ViewModels;

namespace Phantasma.Explorer.Controllers
{
    public class AppsController
    {
        public List<AppViewModel> GetAllApps()
        {
            var context = Explorer.AppServices.GetService<ExplorerDbContext>();

            var appQuery = new AppQueries(context);
            var chainQuery = new ChainQueries(context);
            var txQuery = new TransactionQueries(context);

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
            var context = Explorer.AppServices.GetService<ExplorerDbContext>();

            var appQuery = new AppQueries(context);
            var app = appQuery.QueryApp(appId);
            return AppViewModel.FromApp(app);
        }
    }
}
