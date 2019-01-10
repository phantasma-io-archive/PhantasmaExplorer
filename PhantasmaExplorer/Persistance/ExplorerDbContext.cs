using Microsoft.EntityFrameworkCore;
using Phantasma.Explorer.Domain.Entities;

namespace Phantasma.Explorer.Persistance
{
    public class ExplorerDbContext : DbContext
    {
        public ExplorerDbContext(DbContextOptions<ExplorerDbContext> options)
            : base(options)
        {
        }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<App> Apps { get; set; }
        public DbSet<Block> Blocks { get; set; }
        public DbSet<Chain> Chains { get; set; }
        public DbSet<NFBalance> NfBalances { get; set; }
        public DbSet<Token> Tokens { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ExplorerDbContext).Assembly);
        }
    }
}
