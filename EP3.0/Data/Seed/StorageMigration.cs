using EarnPlatform.Data;
using Microsoft.EntityFrameworkCore;

namespace EarnPlatform.Data.Seed;

/// <summary>
/// One-time (idempotent) relocation of payment screenshots from the old public
/// wwwroot path into private storage. Safe to run on every startup: after the
/// first successful pass there is nothing left to do and the method returns
/// immediately without expensive work.
/// </summary>
public static class StorageMigration
{
    public static async Task MigrateLegacyPaymentScreenshotsAsync(IServiceProvider services)
    {
        var environment = services.GetRequiredService<IWebHostEnvironment>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("StorageMigration");
        var legacyRoot = Path.Combine(environment.WebRootPath, "uploads", "payment-screenshots");
        var privateRoot = Path.Combine(environment.ContentRootPath, "private", "payment-screenshots");
        Directory.CreateDirectory(privateRoot);

        var factory = services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        await using var context = await factory.CreateDbContextAsync();
        // Fast path: no legacy paths in the database AND no leftover files on disk.
        var hasLegacyRows = await context.PaymentApprovals
            .AnyAsync(p => p.PaymentScreenshotPath != null
                && p.PaymentScreenshotPath.StartsWith("/uploads/payment-screenshots/"));
        var hasLegacyFiles = Directory.Exists(legacyRoot)
            && Directory.EnumerateFiles(legacyRoot).Any();

        if (!hasLegacyRows && !hasLegacyFiles)
            return;

        if (hasLegacyRows)
        {
            var approvals = await context.PaymentApprovals
                .Where(p => p.PaymentScreenshotPath != null
                    && p.PaymentScreenshotPath.StartsWith("/uploads/payment-screenshots/"))
                .ToListAsync();

            foreach (var approval in approvals)
            {
                var oldRelative = approval.PaymentScreenshotPath!;
                var fileName = Path.GetFileName(oldRelative);
                if (string.IsNullOrWhiteSpace(fileName)) continue;
                var oldPath = Path.Combine(legacyRoot, fileName);
                var newPath = Path.Combine(privateRoot, fileName);
                if (File.Exists(oldPath))
                {
                    if (!File.Exists(newPath)) File.Move(oldPath, newPath);
                    approval.PaymentScreenshotPath = $"/private/payment-screenshots/{fileName}";
                }
                else if (File.Exists(newPath))
                {
                    approval.PaymentScreenshotPath = $"/private/payment-screenshots/{fileName}";
                }
                else
                {
                    logger.LogWarning("Payment screenshot missing for approval {ApprovalId}: {Path}", approval.Id, oldRelative);
                    approval.PaymentScreenshotPath = null;
                }
            }
            if (approvals.Count > 0) await context.SaveChangesAsync();
        }

        // Move any orphaned legacy files so nothing remains publicly reachable.
        if (Directory.Exists(legacyRoot))
        {
            foreach (var orphan in Directory.EnumerateFiles(legacyRoot))
            {
                var fileName = Path.GetFileName(orphan);
                if (string.IsNullOrWhiteSpace(fileName)) continue;
                var destination = Path.Combine(privateRoot, fileName);
                try
                {
                    if (!File.Exists(destination)) File.Move(orphan, destination);
                    else File.Delete(orphan);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Could not migrate legacy payment screenshot {Path}.", orphan);
                }
            }
        }
    }
}
