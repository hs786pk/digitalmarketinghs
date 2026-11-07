using EarnPlatform.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EarnPlatform.Data.Configurations;

public class WorkTaskConfiguration : IEntityTypeConfiguration<WorkTask>
{
    public void Configure(EntityTypeBuilder<WorkTask> builder)
    {
        builder.Property(t => t.RewardAmount).HasPrecision(18, 2);
        builder.HasIndex(t => t.IsActive);
    }
}