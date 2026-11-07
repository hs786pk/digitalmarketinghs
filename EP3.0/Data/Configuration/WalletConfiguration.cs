using EarnPlatform.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EarnPlatform.Data.Configurations;

public class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        // One wallet per user
        builder.HasIndex(w => w.UserId).IsUnique();

        builder.Property(w => w.CurrentEarning).HasPrecision(18, 2);
        builder.Property(w => w.ReferralEarning).HasPrecision(18, 2);
        builder.Property(w => w.StaffEarning).HasPrecision(18, 2);
        builder.Property(w => w.ApprovedEarning).HasPrecision(18, 2);
        builder.Property(w => w.PendingEarning).HasPrecision(18, 2);
        builder.Property(w => w.RejectedEarning).HasPrecision(18, 2);
        builder.Property(w => w.Balance).HasPrecision(18, 2);

        builder.HasOne(w => w.User)
            .WithOne(u => u.Wallet)
            .HasForeignKey<Wallet>(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
