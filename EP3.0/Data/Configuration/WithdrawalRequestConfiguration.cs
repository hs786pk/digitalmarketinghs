using EarnPlatform.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EarnPlatform.Data.Configurations;

public class WithdrawalRequestConfiguration : IEntityTypeConfiguration<WithdrawalRequest>
{
    public void Configure(EntityTypeBuilder<WithdrawalRequest> builder)
    {
        builder.HasIndex(w => w.UserId);
        builder.HasIndex(w => w.Status);

        builder.Property(w => w.Amount).HasPrecision(18, 2);
        builder.Property(w => w.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(w => w.PaymentMethod).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(w => w.User)
            .WithMany(u => u.WithdrawalRequests)
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(w => w.RelatedWalletTransaction)
            .WithMany()
            .HasForeignKey(w => w.RelatedWalletTransactionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}