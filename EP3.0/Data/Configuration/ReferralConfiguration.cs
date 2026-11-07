using EarnPlatform.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EarnPlatform.Data.Configurations;

public class ReferralConfiguration : IEntityTypeConfiguration<Referral>
{
    public void Configure(EntityTypeBuilder<Referral> builder)
    {
        builder.HasIndex(r => r.ReferralCode);
        builder.HasIndex(r => new { r.ReferrerUserId, r.ReferredUserId }).IsUnique();

        builder.HasOne(r => r.ReferrerUser)
            .WithMany(u => u.ReferralsMade)
            .HasForeignKey(r => r.ReferrerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // No inverse navigation on ReferredUser side to avoid a second cascade path
        builder.HasOne(r => r.ReferredUser)
            .WithMany()
            .HasForeignKey(r => r.ReferredUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
    }
}