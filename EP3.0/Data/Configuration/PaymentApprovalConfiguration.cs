using EarnPlatform.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EarnPlatform.Data.Configurations;

public class PaymentApprovalConfiguration : IEntityTypeConfiguration<PaymentApproval>
{
    public void Configure(EntityTypeBuilder<PaymentApproval> builder)
    {
        builder.HasIndex(p => p.UserId);
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.TransactionId).IsUnique().HasFilter("[Status] <> 'Rejected'").HasDatabaseName("IX_PaymentApprovals_TransactionId_UniqueActive");

        builder.Property(p => p.Amount).HasPrecision(18, 2);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(p => p.User)
            .WithMany(u => u.PaymentApprovals)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Package)
            .WithMany(pk => pk.PaymentApprovals)
            .HasForeignKey(p => p.PackageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}