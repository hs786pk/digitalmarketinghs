using EarnPlatform.Models.Dtos;

namespace EarnPlatform.Services.Interfaces;

public interface IUserDashboardService
{
    /// <summary>
    /// Single round-trip for the user home dashboard: identity, package, wallet, remaining tasks.
    /// Returns null if the authenticated user has no domain account.
    /// </summary>
    Task<UserDashboardDto?> GetAsync(CancellationToken cancellationToken = default);
}