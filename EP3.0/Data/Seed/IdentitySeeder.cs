using EarnPlatform.Factories.Interfaces;
using EarnPlatform.Models;
using EarnPlatform.Repositories.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace EarnPlatform.Data.Seed;

/// <summary>
/// Creates the "User" and "Admin" roles and one initial admin account on
/// startup, if they don't already exist. Admin credentials are read from
/// configuration (appsettings / environment / user-secrets) — never hard-coded.
/// </summary>
public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var adminAccountRepository = services.GetRequiredService<IAdminAccountRepository>();
        var accountFactory = services.GetRequiredService<IAccountFactory>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("IdentitySeeder");

        foreach (var role in new[] { "User", "Admin" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var adminEmail = configuration["SeedAdmin:Email"];
        var adminPassword = configuration["SeedAdmin:Password"];
        var adminName = configuration["SeedAdmin:Name"] ?? "Administrator";

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning("SeedAdmin:Email / SeedAdmin:Password not configured — skipping admin seed. " +
                               "Set them in user-secrets or environment variables to seed an initial admin.");
            return;
        }

        var existing = await userManager.FindByEmailAsync(adminEmail);
        if (existing != null)
        {
            // The account already exists (e.g. created before SeedAdmin:Email was
            // configured, or via an earlier partial run). Don't just skip — make
            // sure it actually has the Admin role, since a user can exist with
            // zero roles and silently fail every [Authorize(Policy = "RequireAdminRole")]
            // check without any obvious error.
            if (!await userManager.IsInRoleAsync(existing, "Admin"))
            {
                var addToRoleResult = await userManager.AddToRoleAsync(existing, "Admin");
                if (addToRoleResult.Succeeded)
                    logger.LogWarning("Existing account {Email} was missing the Admin role — assigned it now.", adminEmail);
                else
                    logger.LogError("Failed to assign Admin role to existing account {Email}: {Errors}",
                        adminEmail, string.Join("; ", addToRoleResult.Errors.Select(e => e.Description)));
            }

            return;
        }

        var applicationUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(applicationUser, adminPassword);
        if (!result.Succeeded)
        {
            logger.LogError("Failed to seed admin user: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(applicationUser, "Admin");

        // Factory Pattern: admin profile creation goes through IAccountFactory, same as regular users
        // Factory Pattern: admin profile creation goes through IAccountFactory, same as regular users
        var adminAccount = accountFactory.CreateAdminAccount(applicationUser.Id, adminName, adminEmail, "SuperAdmin");
        await adminAccountRepository.AddAsync(adminAccount); // persists immediately — no separate SaveChanges


        logger.LogInformation("Seeded initial admin account: {Email}", adminEmail);
    }
}