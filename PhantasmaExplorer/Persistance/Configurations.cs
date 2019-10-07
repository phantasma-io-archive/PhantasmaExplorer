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
            builder.OwnsMany(p => p.TokenBalance, a =>
            {
                a.HasForeignKey("Address");
                a.Property(ca => ca.Chain);
                a.Property(ca => ca.TokenSymbol);
                a.Property(ca => ca.Amount);
                a.HasKey("Address", "Chain", "TokenSymbol", "Amount");
            });
        }
    }

    public class NonFungibleTokenConfiguration : IEntityTypeConfiguration<NonFungibleToken>
    {
        public void Configure(EntityTypeBuilder<NonFungibleToken> builder)
        {
            builder.HasKey(e => e.Id);

            builder.HasOne(p => p.Account)
                .WithMany(p => p.NonFungibleTokens)
                .HasForeignKey(p => p.AccountAddress);
        }
    }

    public class ChainConfiguration : IEntityTypeConfiguration<Chain>
    {
        public void Configure(EntityTypeBuilder<Chain> builder)
        {
            builder.HasKey(e => e.Address);
        }
    }

    public class BlockConfiguration : IEntityTypeConfiguration<Block>
    {
        public void Configure(EntityTypeBuilder<Block> builder)
        {
            builder.HasKey(e => e.Hash);

            builder.HasOne(d => d.Chain)
                .WithMany(p => p.Blocks)
                .HasForeignKey(d => d.ChainAddress)
            .HasConstraintName("FK_Blocks_Chains");
        }
    }

    public class TokenConfiguration : IEntityTypeConfiguration<Token>
    {
        public void Configure(EntityTypeBuilder<Token> builder)
        {
            builder.HasKey(e => e.Symbol);

            builder.OwnsMany(p => p.MetadataList, a =>
            {
                a.HasForeignKey("Symbol");
                a.Property(ca => ca.Key);
                a.Property(ca => ca.Value);
                a.HasKey("Symbol", "Key", "Value");
            });
        }
    }

    public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
    {
        public void Configure(EntityTypeBuilder<Transaction> builder)
        {
            builder.HasKey(e => e.Hash);

            builder.HasOne(d => d.Block)
                .WithMany(p => p.Transactions)
                .HasForeignKey(d => d.BlockHash)
                .HasConstraintName("FK_Transactions_Blocks");

            builder.OwnsMany(p => p.Events, a =>
            {
                a.HasForeignKey("Hash");
                a.Property(ca => ca.Data);
                a.Property(ca => ca.EventAddress);
                a.Property(ca => ca.EventKind);
                a.Property(ca => ca.Contract);
                a.HasKey("Hash", "Data", "EventAddress", "EventKind", "Contract");
            });
        }
    }

    public class AccountTransactionConfiguration : IEntityTypeConfiguration<AccountTransaction>
    {
        public void Configure(EntityTypeBuilder<AccountTransaction> builder)
        {
            builder.HasKey(at => new { at.AccountId, at.TransactionId });

            builder.HasOne(at => at.Account)
                .WithMany(b => b.AccountTransactions)
                .HasForeignKey(at => at.AccountId);

            builder.HasOne(at => at.Transaction)
                .WithMany(c => c.AccountTransactions)
                .HasForeignKey(at => at.TransactionId);
        }
    }
}
