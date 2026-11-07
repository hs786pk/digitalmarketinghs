using System.ComponentModel.DataAnnotations;
using EarnPlatform.Models;

namespace EarnPlatform.Api.Dtos;

public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? ReferralCode { get; set; }
}

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime AccessExpiresAtUtc { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshExpiresAtUtc { get; set; }
    public UserSummaryDto User { get; set; } = default!;
}
public class UserSummaryDto
{
    public int? UserAccountId { get; set; }
    public string ApplicationUserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public string? AccountStatus { get; set; }
    public int? SelectedPackageId { get; set; }
    public string? ReferralCode { get; set; }
}

public class ApiErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public IEnumerable<string>? Errors { get; set; }
    public string? RequestId { get; set; }
}

public class RefreshRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? DeviceName { get; set; }
}

public class LogoutRequest
{
    /// <summary>If provided, revokes this refresh token. If omitted with AllDevices=true, revokes all.</summary>
    public string? RefreshToken { get; set; }
    public bool AllDevices { get; set; }
}