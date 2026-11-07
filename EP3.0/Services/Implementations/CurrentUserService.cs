using System.Security.Claims;
using EarnPlatform.Models;
using EarnPlatform.Repositories.Interfaces;
using EarnPlatform.Services.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;

namespace EarnPlatform.Services.Implementations;

/// <summary>
/// Resolves the domain UserAccount/AdminAccount for the currently authenticated
/// Identity user. Results are cached for the lifetime of the DI scope
/// (Blazor Server circuit / HTTP request) so multiple components do not
/// each hit the database.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IAdminAccountRepository _adminAccountRepository;

    private string? _applicationUserId;
    private bool _applicationUserIdLoaded;

    private UserAccount? _userAccount;
    private bool _userAccountLoaded;

    private AdminAccount? _adminAccount;
    private bool _adminAccountLoaded;

    public CurrentUserService(
        AuthenticationStateProvider authStateProvider,
        IUserAccountRepository userAccountRepository,
        IAdminAccountRepository adminAccountRepository)
    {
        _authStateProvider = authStateProvider;
        _userAccountRepository = userAccountRepository;
        _adminAccountRepository = adminAccountRepository;
    }

    public async Task<UserAccount?> GetCurrentUserAccountAsync(CancellationToken cancellationToken = default)
    {
        if (_userAccountLoaded)
            return _userAccount;

        var applicationUserId = await GetApplicationUserIdAsync();
        _userAccount = applicationUserId == null
            ? null
            : await _userAccountRepository.GetByApplicationUserIdAsync(applicationUserId, cancellationToken);

        _userAccountLoaded = true;
        return _userAccount;
    }

    public async Task<AdminAccount?> GetCurrentAdminAccountAsync(CancellationToken cancellationToken = default)
    {
        if (_adminAccountLoaded)
            return _adminAccount;

        var applicationUserId = await GetApplicationUserIdAsync();
        _adminAccount = applicationUserId == null
            ? null
            : await _adminAccountRepository.GetByApplicationUserIdAsync(applicationUserId, cancellationToken);

        _adminAccountLoaded = true;
        return _adminAccount;
    }

    /// <summary>
    /// Call after mutating the current user's profile (e.g. package change,
    /// approval) so the next read reloads from the database.
    /// </summary>
    public void Invalidate()
    {
        _userAccountLoaded = false;
        _userAccount = null;
        _adminAccountLoaded = false;
        _adminAccount = null;
        // Keep _applicationUserId — identity does not change mid-circuit.
    }

    private async Task<string?> GetApplicationUserIdAsync()
    {
        if (_applicationUserIdLoaded)
            return _applicationUserId;

        var state = await _authStateProvider.GetAuthenticationStateAsync();
        _applicationUserId = state.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        _applicationUserIdLoaded = true;
        return _applicationUserId;
    }
}