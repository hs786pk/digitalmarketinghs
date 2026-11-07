using EarnPlatform.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EarnPlatform.Data.Configuration;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => x.ApplicationUserId);
        builder.HasIndex(x => new { x.ApplicationUserId, x.RevokedAtUtc });
    }
}