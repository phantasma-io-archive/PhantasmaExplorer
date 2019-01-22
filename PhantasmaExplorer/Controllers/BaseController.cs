using Phantasma.Explorer.Persistance;

namespace Phantasma.Explorer.Controllers
{
    public abstract class BaseController
    {
        protected readonly ExplorerDbContext _context;

        protected BaseController(ExplorerDbContext context)
        {
            _context = context;
        }
    }
}
