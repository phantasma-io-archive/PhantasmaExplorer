using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Phantasma.Explorer.Domain.Entities;

namespace Phantasma.Explorer.Persistance
{
    public class AccountConfiguration : IEntityTypeConfiguration<Account>
    {
        public void Configure(EntityTypeBuilder<Account> builder)
        {
            builder.HasKey(e => e.Address);
        }
    }

    public class AppConfiguration : IEntityTypeConfiguration<App>
    {
        public void Configure(EntityTypeBuilder<App> builder)
        {
            builder.HasKey(e => e.Id);
        }
    }

    public class BlockConfiguration : IEntityTypeConfiguration<Block>
    {
        public void Configure(EntityTypeBuilder<Block> builder)
        {
            builder.HasKey(e => e.Hash);

            builder.HasOne(p => p.Chain)
                .WithMany(p => p.Blocks)
                .HasForeignKey(p => p.ChainAddress);
        }
    }

    public class ChainConfiguration : IEntityTypeConfiguration<Chain>
    {
        public void Configure(EntityTypeBuilder<Chain> builder)
        {
            builder.HasKey(e => e.Address);

            builder.OwnsMany(p => p.Blocks);
        }
    }


    public class NFBalanceConfiguration : IEntityTypeConfiguration<NFBalance>
    {
        public void Configure(EntityTypeBuilder<NFBalance> builder)
        {
            builder.HasKey(p => p.Id);
        }
    }

    public class TokenConfiguration : IEntityTypeConfiguration<Token>
    {
        public void Configure(EntityTypeBuilder<Token> builder)
        {
            builder.HasKey(e => e.Symbol);
        }
    }

    public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
    {
        public void Configure(EntityTypeBuilder<Transaction> builder)
        {
            builder.HasKey(e => e.Hash);
            builder.OwnsMany(p => p.Events);

            builder.HasOne(p => p.Block)
                .WithMany(p => p.Transactions)
                .HasForeignKey(p => p.BlockHash);
        }
    }
}
