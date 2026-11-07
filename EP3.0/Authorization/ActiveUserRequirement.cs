using System.Security.Claims;
using EarnPlatform.Models;
using EarnPlatform.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace EarnPlatform.Authorization;

public sealed class ActiveUserRequirement : IAuthorizationRequirement { }

/// <summary>
/// Succeeds only when the authenticated non-admin user has AccountStatus.Active.
/// Used by web (cookie) and mobile API (JWT) via RequireActiveUser / RequireActiveUserApi.
/// </summary>
public sealed class ActiveUserHandler : AuthorizationHandler<ActiveUserRequirement>
{
    private readonly IUserAccountRepository _users;

    public ActiveUserHandler(IUserAccountRepository users) => _users = users;

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ActiveUserRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return;

        // Admins are not subject to the user "Active" gate
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return;
        }

        var identityId =
            context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");

        if (string.IsNullOrEmpty(identityId))
            return;

        var user = await _users.GetByApplicationUserIdAsync(identityId);
        if (user == null)
            return;

        // Only Active accounts may pass. Pending / Rejected / Suspended fail the policy.
        if (user.AccountStatus == AccountStatus.Active)
            context.Succeed(requirement);
    }
}