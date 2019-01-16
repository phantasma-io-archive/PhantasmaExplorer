using System.Linq;
using Microsoft.EntityFrameworkCore;
using Phantasma.Explorer.Domain.Entities;

namespace Phantasma.Explorer.Utils
{
   public static class QueryExtensions
    {
        public static IQueryable<Block> IncludeTransactions(this IQueryable<Block> query)
        {
            return query.Include(x => x.Transactions);
        }
    }
}
