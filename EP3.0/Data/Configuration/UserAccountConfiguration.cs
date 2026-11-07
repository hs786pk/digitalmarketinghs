using EarnPlatform.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EarnPlatform.Data.Configurations;

public class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.ReferralCode).IsUnique();
        builder.HasIndex(u => u.ApplicationUserId).IsUnique();

        // One ApplicationUser (Identity login) <-> one UserAccount (profile)
        builder.HasOne(u => u.ApplicationUser)
            .WithMany()
            .HasForeignKey(u => u.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Self-referencing referral relationship — restrict delete to avoid cascade cycles
        builder.HasOne(u => u.ReferredByUser)
            .WithMany(u => u.ReferredUsers)
            .HasForeignKey(u => u.ReferredByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(u => u.SelectedPackage)
            .WithMany(p => p.Subscribers)
            .HasForeignKey(u => u.SelectedPackageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(u => u.AccountStatus)
            .HasConversion<string>()
            .HasMaxLength(30);
    }
}