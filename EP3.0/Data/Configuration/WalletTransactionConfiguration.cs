using EarnPlatform.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EarnPlatform.Data.Configurations;

public class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
{
    public void Configure(EntityTypeBuilder<WalletTransaction> builder)
    {
        builder.HasIndex(t => t.UserId);
        builder.HasIndex(t => t.Status);

        builder.Property(t => t.Amount).HasPrecision(18, 2);

        builder.HasOne(t => t.User)
            .WithMany(u => u.WalletTransactions)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(t => t.TransactionType).HasConversion<string>().HasMaxLength(40);
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
    }
}
