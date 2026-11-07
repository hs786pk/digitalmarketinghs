using EarnPlatform.Models;

namespace EarnPlatform.Services.Interfaces;

public interface IPackageService
{
    Task<List<Package>> GetActivePackagesAsync(CancellationToken cancellationToken = default);
    Task<List<Package>> GetAllPackagesAsync(CancellationToken cancellationToken = default);
    Task<Package?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Package> CreatePackageAsync(string name, decimal price, int level, int dailyAdLimit, string? description, CancellationToken cancellationToken = default);
    Task UpdatePackageAsync(int id, string name, decimal price, int level, int dailyAdLimit, string? description, bool isActive, CancellationToken cancellationToken = default);
    Task SetActiveStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default);
}