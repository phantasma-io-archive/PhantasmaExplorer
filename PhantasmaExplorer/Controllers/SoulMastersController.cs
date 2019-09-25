using Microsoft.Extensions.DependencyInjection;
using Phantasma.Explorer.Application;
using Phantasma.Explorer.Application.Queries;
using Phantasma.Explorer.Persistance;
using Phantasma.Explorer.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace Phantasma.Explorer.Controllers
{
    public class SoulMastersController : BaseController
    {
        public SoulMastersController() : base(Explorer.AppServices.GetService<ExplorerDbContext>()) { }

        public List<AddressViewModel> GetSoulMasters(int currentPage, int pageSize = AppSettings.PageSize)
        {
            var addressQueries = new AccountQueries(_context);
            var query = addressQueries.QuerySoulMasters().Skip((currentPage - 1) * pageSize).Take(pageSize);
            return query.AsEnumerable()
               .Select(AddressViewModel.FromSoulMaster)
               .ToList();
        }

        public int GetSoulsMasterCount()
        {
            var addressQueries = new AccountQueries(_context);

            return addressQueries.QuerySoulMasters().Count();
        }
    }
}
