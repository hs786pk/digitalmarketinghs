using EarnPlatform.Models;

namespace EarnPlatform.Factories.Interfaces;

/// <summary>
/// Factory Pattern: centralizes how new Package entities are constructed so
/// admin-facing code never hand-assembles a Package with inconsistent defaults
/// (e.g. forgetting IsActive = true, or CreatedAt).
/// </summary>
public interface IPackageFactory
{
    Package CreatePackage(string name, decimal price, int level, int dailyAdLimit, string? description, bool isActive = true);
}