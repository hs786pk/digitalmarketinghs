using EarnPlatform.Common.Exceptions;
using EarnPlatform.Factories.Interfaces;
using EarnPlatform.Models;
using EarnPlatform.Repositories.Interfaces;
using EarnPlatform.Services.Interfaces;

namespace EarnPlatform.Services.Implementations;

public class PackageService : IPackageService
{
    private readonly IPackageRepository _packageRepository;
    private readonly IPackageFactory _packageFactory;

    public PackageService(IPackageRepository packageRepository, IPackageFactory packageFactory)
    {
        _packageRepository = packageRepository;
        _packageFactory = packageFactory;
    }

    public Task<List<Package>> GetActivePackagesAsync(CancellationToken cancellationToken = default) =>
        _packageRepository.GetActivePackagesAsync(cancellationToken);

    public Task<List<Package>> GetAllPackagesAsync(CancellationToken cancellationToken = default) =>
        _packageRepository.GetAllAsync(cancellationToken);

    public Task<Package?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _packageRepository.GetByIdAsync(id, cancellationToken);

    public async Task<Package> CreatePackageAsync(
        string name, decimal price, int level, int dailyAdLimit, string? description,
        CancellationToken cancellationToken = default)
    {
        name = name?.Trim() ?? string.Empty;
        description = description?.Trim();
        ValidatePackage(name, price, level, dailyAdLimit);
        if (await _packageRepository.NameExistsAsync(name, cancellationToken: cancellationToken))
            throw new ConflictException($"A package named '{name}' already exists.");

        var package = _packageFactory.CreatePackage(name, price, level, dailyAdLimit, description);
        await _packageRepository.AddAsync(package, cancellationToken);
        return package;
    }

    public async Task UpdatePackageAsync(
        int id, string name, decimal price, int level, int dailyAdLimit, string? description, bool isActive,
        CancellationToken cancellationToken = default)
    {
        name = name?.Trim() ?? string.Empty;
        description = description?.Trim();
        ValidatePackage(name, price, level, dailyAdLimit);
        var package = await _packageRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Package not found.");

        if (await _packageRepository.NameExistsAsync(name, excludeId: id, cancellationToken))
            throw new ConflictException($"A package named '{name}' already exists.");

        package.Name = name;
        package.Price = price;
        package.Level = level;
        package.DailyAdLimit = dailyAdLimit;
        package.Description = description;
        package.IsActive = isActive;
        package.UpdatedAt = DateTime.UtcNow;

        await _packageRepository.UpdateAsync(package, cancellationToken);
    }

    public async Task SetActiveStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default)
    {
        var package = await _packageRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Package not found.");

        package.IsActive = isActive;
        package.UpdatedAt = DateTime.UtcNow;
        await _packageRepository.UpdateAsync(package, cancellationToken);
    }

    private static void ValidatePackage(string name, decimal price, int level, int dailyAdLimit)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
            throw new ValidationException("Package name is required and must be 100 characters or fewer.");
        if (price <= 0)
            throw new ValidationException("Package price must be positive.");
        if (level < 0)
            throw new ValidationException("Package level cannot be negative.");
        if (dailyAdLimit <= 0)
            throw new ValidationException("Daily ad limit must be at least 1.");
    }
}