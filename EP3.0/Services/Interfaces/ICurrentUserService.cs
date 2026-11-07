using EarnPlatform.Models;

namespace EarnPlatform.Services.Interfaces;

public interface ICurrentUserService
{
    Task<UserAccount?> GetCurrentUserAccountAsync(CancellationToken cancellationToken = default);
    Task<AdminAccount?> GetCurrentAdminAccountAsync(CancellationToken cancellationToken = default);

    /// <summary>Clear circuit-scoped cache after the current user is mutated.</summary>
    void Invalidate();
}