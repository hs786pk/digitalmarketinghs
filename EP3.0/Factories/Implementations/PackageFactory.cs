using EarnPlatform.Factories.Interfaces;
using EarnPlatform.Models;

namespace EarnPlatform.Factories.Implementations;

public class PackageFactory : IPackageFactory
{
    public Package CreatePackage(string name, decimal price, int level, int dailyAdLimit, string? description, bool isActive = true)
    {
        return new Package
        {
            Name = name,
            Price = price,
            Level = level,
            DailyAdLimit = dailyAdLimit,
            Description = description,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
    }
}