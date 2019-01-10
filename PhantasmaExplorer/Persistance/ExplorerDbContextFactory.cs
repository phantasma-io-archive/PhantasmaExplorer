using Microsoft.EntityFrameworkCore;
using Phantasma.Explorer.Persistance.Infrastructure;

namespace Phantasma.Explorer.Persistance
{
    public class ExplorerDbContextFactory : DesignTimeDbContextFactoryBase<ExplorerDbContext>
    {
        protected override ExplorerDbContext CreateNewInstance(DbContextOptions<ExplorerDbContext> options)
        {
            return new ExplorerDbContext(options);
        }
    }
}
