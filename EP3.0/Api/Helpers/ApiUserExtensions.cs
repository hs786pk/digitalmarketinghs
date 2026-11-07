using System.Security.Claims;
using EarnPlatform.Models;
using EarnPlatform.Repositories.Interfaces;

namespace EarnPlatform.Api.Helpers;

public static class ApiUserExtensions
{
    public static string? GetApplicationUserId(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");
    }

    public static async Task<UserAccount?> GetUserAccountAsync(
        this ClaimsPrincipal user,
        IUserAccountRepository userAccountRepository,
        CancellationToken cancellationToken = default)
    {
        var appUserId = user.GetApplicationUserId();
        if (string.IsNullOrEmpty(appUserId))
            return null;

        return await userAccountRepository.GetByApplicationUserIdAsync(appUserId, cancellationToken);
    }
}