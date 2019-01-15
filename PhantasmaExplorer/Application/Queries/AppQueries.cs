using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Phantasma.Explorer.Persistance;

namespace Phantasma.Explorer.Application.Queries
{
    public class AppQueries
    {
        private readonly ExplorerDbContext _context;

        public AppQueries(ExplorerDbContext context)
        {
            _context = context;
            _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        public IEnumerable<Domain.Entities.App> QueryApps()
        {
            return _context.Apps;
        }

        public Domain.Entities.App QueryApp(string appIdentifier)
        {
            return _context.Apps.SingleOrDefault(p => p.Id.Equals(appIdentifier) || p.Title.Equals(appIdentifier));
        }
    }
}
