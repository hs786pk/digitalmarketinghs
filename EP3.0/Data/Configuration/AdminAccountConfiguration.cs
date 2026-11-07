using EarnPlatform.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EarnPlatform.Data.Configurations;

public class AdminAccountConfiguration : IEntityTypeConfiguration<AdminAccount>
{
    public void Configure(EntityTypeBuilder<AdminAccount> builder)
    {
        builder.HasIndex(a => a.Email).IsUnique();
        builder.HasIndex(a => a.ApplicationUserId).IsUnique();

        builder.HasOne(a => a.ApplicationUser)
            .WithMany()
            .HasForeignKey(a => a.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}