using EarnPlatform.Data;
using EarnPlatform.Models;
using EarnPlatform.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EarnPlatform.Repositories.Implementations;

public class PackageRepository : GenericRepository<Package>, IPackageRepository
{
    public PackageRepository(IDbContextFactory<ApplicationDbContext> factory) : base(factory) { }

    public async Task<List<Package>> GetActivePackagesAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await Factory.CreateDbContextAsync(cancellationToken);
        return await context.Packages
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Level)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        await using var context = await Factory.CreateDbContextAsync(cancellationToken);
        return await context.Packages.AnyAsync(
            p => p.Name == name && (excludeId == null || p.Id != excludeId),
            cancellationToken);
    }
}