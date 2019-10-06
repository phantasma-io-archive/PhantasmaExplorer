using Microsoft.EntityFrameworkCore;
using Phantasma.Explorer.Domain.Entities;
using System;

namespace Phantasma.Explorer.Persistance
{
    public class ExplorerDbContext : DbContext
    {
        public ExplorerDbContext(DbContextOptions options) : base(options) { }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<Block> Blocks { get; set; }
        public DbSet<Chain> Chains { get; set; }
        public DbSet<NonFungibleToken> NonFungibleTokens { get; set; }
        public DbSet<Token> Tokens { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ExplorerDbContext).Assembly);
            modelBuilder.Entity<Chain>().Property(e => e.Contracts)
            .HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
