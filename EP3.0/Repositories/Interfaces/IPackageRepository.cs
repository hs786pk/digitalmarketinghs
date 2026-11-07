using EarnPlatform.Models;

namespace EarnPlatform.Repositories.Interfaces;

public interface IPackageRepository : IGenericRepository<Package>
{
    Task<List<Package>> GetActivePackagesAsync(CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);
}