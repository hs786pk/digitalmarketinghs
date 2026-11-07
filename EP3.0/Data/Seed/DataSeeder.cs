using EarnPlatform.Data;
using EarnPlatform.Factories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EarnPlatform.Data.Seed;

/// <summary>
/// Seeds Packages and WorkTasks on first run, per spec section 38. Only the
/// Basic package's values are mandated by the spec (650 PKR / Level 0 / 1 ad
/// per day) — Standard/Premium/Super get reasonable configurable starting
/// values that the admin can change immediately from /admin/packages.
/// </summary>
public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var factory = services.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        await using var context = await factory.CreateDbContextAsync();
        var packageFactory = services.GetRequiredService<IPackageFactory>();
        var workTaskFactory = services.GetRequiredService<IWorkTaskFactory>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DataSeeder");

        // Ensure every existing account has exactly one wallet. New registrations also create one transactionally.
        var userIds = await context.UserAccounts.Select(u => u.Id).ToListAsync();
        var walletUserIds = await context.Wallets.Select(w => w.UserId).ToHashSetAsync();
        var missingWallets = userIds.Where(id => !walletUserIds.Contains(id))
            .Select(id => new EarnPlatform.Models.Wallet { UserId = id, UpdatedAt = DateTime.UtcNow })
            .ToList();
        if (missingWallets.Count > 0)
        {
            await context.Wallets.AddRangeAsync(missingWallets);
            await context.SaveChangesAsync();
            logger.LogInformation("Created {Count} missing user wallets.", missingWallets.Count);
        }

        if (!await context.Packages.AnyAsync())
        {
            var packages = new[]
            {
                // Factory Pattern: every seeded package goes through IPackageFactory, same as admin-created ones
                packageFactory.CreatePackage("Basic", 650m, level: 0, dailyAdLimit: 1,
                    description: "Perfect for getting started with daily earning tasks."),
                packageFactory.CreatePackage("Standard", 1500m, level: 1, dailyAdLimit: 3,
                    description: "More daily tasks and a higher earning ceiling."),
                packageFactory.CreatePackage("Premium", 3000m, level: 2, dailyAdLimit: 5,
                    description: "Premium access with expanded daily limits."),
                packageFactory.CreatePackage("Super", 5000m, level: 3, dailyAdLimit: 10,
                    description: "Our top-tier package for maximum daily earnings."),
            };

            await context.Packages.AddRangeAsync(packages);
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded {Count} packages.", packages.Length);
        }

        if (!await context.WorkTasks.AnyAsync())
        {
            var tasks = new[]
            {
                // Factory Pattern: every seeded task goes through IWorkTaskFactory, same as admin-created ones
                workTaskFactory.CreateWorkTask("Watch Brand Ad — 30s", "Watch a short promotional video to completion.", "https://www.youtube.com/embed/dQw4w9WgXcQ", 20m),
                workTaskFactory.CreateWorkTask("Watch Product Demo", "View a product demonstration video.", "https://www.youtube.com/embed/dQw4w9WgXcQ", 25m),
                workTaskFactory.CreateWorkTask("Daily Sponsor Video", "Complete today's sponsor video task.", "https://www.youtube.com/embed/dQw4w9WgXcQ", 20m),
                workTaskFactory.CreateWorkTask("App Promo Task", "Watch a mobile app promotional clip.", "https://www.youtube.com/embed/dQw4w9WgXcQ", 30m),
                workTaskFactory.CreateWorkTask("Featured Ad", "Watch this week's featured advertisement.", "https://www.youtube.com/embed/dQw4w9WgXcQ", 20m),
            };

            await context.WorkTasks.AddRangeAsync(tasks);
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded {Count} work tasks.", tasks.Length);
        }
    }
}