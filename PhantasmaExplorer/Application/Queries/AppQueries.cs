using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Phantasma.Explorer.Persistance;

namespace Phantasma.Explorer.Application.Queries
{
    public class AppQueries
    {
        private readonly ExplorerDbContext _context;

        public AppQueries()
        {
            _context = Explorer.AppServices.GetService<ExplorerDbContext>();
            _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        public ICollection<Domain.Entities.App> QueryApps()
        {
            return _context.Apps.ToList();
        }

        public Domain.Entities.App QueryApp(string appIdentifier)
        {
            return _context.Apps.SingleOrDefault(p => p.Id.Equals(appIdentifier) || p.Title.Equals(appIdentifier));
        }
    }
}
