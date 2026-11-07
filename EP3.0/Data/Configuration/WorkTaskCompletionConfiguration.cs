using EarnPlatform.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EarnPlatform.Data.Configurations;

public class WorkTaskCompletionConfiguration : IEntityTypeConfiguration<WorkTaskCompletion>
{
    public void Configure(EntityTypeBuilder<WorkTaskCompletion> builder)
    {
        builder.HasIndex(c => c.UserId);
        builder.HasIndex(c => new { c.UserId, c.Status, c.StartedAt });
        builder.HasIndex(c => new { c.UserId, c.WorkTaskId, c.CompletedAt });

        builder.Property(c => c.RewardAmount).HasPrecision(18, 2);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(c => c.User)
            .WithMany(u => u.WorkTaskCompletions)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.WorkTask)
            .WithMany(t => t.Completions)
            .HasForeignKey(c => c.WorkTaskId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}