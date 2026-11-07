using EarnPlatform.Models;

namespace EarnPlatform.Services.Interfaces;

public record RegisterResult(bool Succeeded, string? UserId, IEnumerable<string> Errors);

public record LoginResult(
    bool Succeeded,
    bool IsLockedOut,
    string? ErrorMessage,
    bool IsAdmin = false,
    AccountStatus? AccountStatus = null,
    int? SelectedPackageId = null);

public interface IAuthService
{
    Task<RegisterResult> RegisterUserAsync(string firstName, string lastName, string email, string password, string? referralCode, CancellationToken cancellationToken = default);
    Task<LoginResult> LoginAsync(string email, string password, bool rememberMe, CancellationToken cancellationToken = default);
    Task LogoutAsync();
}