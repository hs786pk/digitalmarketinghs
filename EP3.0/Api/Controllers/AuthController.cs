using EarnPlatform.Api.Dtos;
using EarnPlatform.Api.Services;
using EarnPlatform.Data;
using EarnPlatform.Models;
using EarnPlatform.Repositories.Interfaces;
using EarnPlatform.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace EarnPlatform.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IAuthService _authService;
    private readonly JwtTokenService _jwtTokenService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IUserAccountRepository userAccountRepository,
        IAuthService authService,
        JwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _userAccountRepository = userAccountRepository;
        _authService = authService;
        _jwtTokenService = jwtTokenService;
    }

    /// <summary>
    /// Mobile login. Validates password without creating a cookie session
    /// (CheckPasswordSignInAsync). Website cookie login stays separate.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var email = request.Email.Trim();
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return Unauthorized(new ApiErrorResponse
            {
                Message = "Invalid email or password."
            });
        }

        // Does NOT set cookie — API uses JWT only
        var result = await _signInManager.CheckPasswordSignInAsync(
            user, request.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            return StatusCode(StatusCodes.Status423Locked, new ApiErrorResponse
            {
                Message = "Your account is temporarily locked due to multiple failed attempts. Please try again later."
            });
        }

        if (!result.Succeeded)
        {
            return Unauthorized(new ApiErrorResponse
            {
                Message = "Invalid email or password."
            });
        }

        var userAccount = await _userAccountRepository.GetByApplicationUserIdAsync(
            user.Id, cancellationToken);
        if (userAccount != null)
        {
            userAccount.LastLoginAt = DateTime.UtcNow;
            await _userAccountRepository.UpdateAsync(userAccount, cancellationToken);
        }
        // Suspended / rejected users must not receive tokens
        if (userAccount?.AccountStatus is AccountStatus.Suspended or AccountStatus.Rejected)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ApiErrorResponse
            {
                Message = "Your account is not allowed to sign in."
            });
        }

        var roles = await _userManager.GetRolesAsync(user);
        var (access, accessExp) = _jwtTokenService.CreateAccessToken(user, userAccount, roles);
        var (refresh, refreshEntity) = await _jwtTokenService.IssueRefreshTokenAsync(
            user.Id,
            Request.Headers.UserAgent.ToString(),
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);

        return Ok(new AuthResponse
        {
            AccessToken = access,
            AccessExpiresAtUtc = accessExp,
            RefreshToken = refresh,
            RefreshExpiresAtUtc = refreshEntity.ExpiresAtUtc,
            User = MapUser(user, userAccount, roles)
        });
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<AuthResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var registerResult = await _authService.RegisterUserAsync(
            request.FirstName,
            request.LastName,
            request.Email,
            request.Password,
            request.ReferralCode,
            cancellationToken);

        if (!registerResult.Succeeded)
        {
            return BadRequest(new ApiErrorResponse
            {
                Message = "Registration failed.",
                Errors = registerResult.Errors
            });
        }

        // Auto-login after register (JWT only, no cookie)
        var user = await _userManager.FindByEmailAsync(request.Email.Trim());
        if (user == null)
            return StatusCode(500, new ApiErrorResponse { Message = "Registration succeeded but user was not found." });

        var userAccount = await _userAccountRepository.GetByApplicationUserIdAsync(
            user.Id, cancellationToken);
        var roles = await _userManager.GetRolesAsync(user);
        var (access, accessExp) = _jwtTokenService.CreateAccessToken(user, userAccount, roles);
        var (refresh, refreshEntity) = await _jwtTokenService.IssueRefreshTokenAsync(
            user.Id,
            Request.Headers.UserAgent.ToString(),
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);

        return Ok(new AuthResponse
        {
            AccessToken = access,
            AccessExpiresAtUtc = accessExp,
            RefreshToken = refresh,
            RefreshExpiresAtUtc = refreshEntity.ExpiresAtUtc,
            User = MapUser(user, userAccount, roles)
        });
    }

    [HttpGet("me")]
    [Authorize(Policy = "RequireAuthenticatedApi")]
    public async Task<ActionResult<UserSummaryDto>> Me(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return Unauthorized();

        var userAccount = await _userAccountRepository.GetByApplicationUserIdAsync(
            userId, cancellationToken);
        var roles = await _userManager.GetRolesAsync(user);

        return Ok(MapUser(user, userAccount, roles));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<AuthResponse>> Refresh(
    [FromBody] RefreshRequest request,
    CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return Unauthorized(new ApiErrorResponse { Message = "Invalid refresh token." });

        var rotated = await _jwtTokenService.RotateRefreshTokenAsync(
            request.RefreshToken.Trim(),
            request.DeviceName ?? Request.Headers.UserAgent.ToString(),
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);

        if (rotated == null)
            return Unauthorized(new ApiErrorResponse { Message = "Invalid or expired refresh token." });

        var (rawRefresh, entity) = rotated.Value;
        var user = await _userManager.FindByIdAsync(entity.ApplicationUserId);
        if (user == null)
            return Unauthorized(new ApiErrorResponse { Message = "User not found." });

        var userAccount = await _userAccountRepository.GetByApplicationUserIdAsync(user.Id, cancellationToken);
        if (userAccount?.AccountStatus is AccountStatus.Suspended or AccountStatus.Rejected)
        {
            await _jwtTokenService.RevokeAllForUserAsync(user.Id, cancellationToken);
            return StatusCode(StatusCodes.Status403Forbidden, new ApiErrorResponse
            {
                Message = "Your account is not allowed to sign in."
            });
        }

        var roles = await _userManager.GetRolesAsync(user);
        var (access, accessExp) = _jwtTokenService.CreateAccessToken(user, userAccount, roles);

        return Ok(new AuthResponse
        {
            AccessToken = access,
            AccessExpiresAtUtc = accessExp,
            RefreshToken = rawRefresh,
            RefreshExpiresAtUtc = entity.ExpiresAtUtc,
            User = MapUser(user, userAccount, roles)
        });
    }

    [HttpPost("logout")]
    [Authorize(Policy = "RequireAuthenticatedApi")]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequest? request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        if (request?.AllDevices == true)
        {
            await _jwtTokenService.RevokeAllForUserAsync(userId, cancellationToken);
            return Ok(new { message = "All sessions revoked." });
        }

        if (!string.IsNullOrWhiteSpace(request?.RefreshToken))
            await _jwtTokenService.RevokeRefreshTokenAsync(request.RefreshToken.Trim(), cancellationToken);

        return Ok(new { message = "Logged out." });
    }

    private static UserSummaryDto MapUser(
        ApplicationUser user,
        UserAccount? account,
        IList<string> roles)
    {
        return new UserSummaryDto
        {
            ApplicationUserId = user.Id,
            Email = user.Email ?? string.Empty,
            UserAccountId = account?.Id,
            FirstName = account?.FirstName ?? string.Empty,
            LastName = account?.LastName ?? string.Empty,
            IsAdmin = roles.Contains("Admin"),
            AccountStatus = account?.AccountStatus.ToString(),
            SelectedPackageId = account?.SelectedPackageId,
            ReferralCode = account?.ReferralCode
        };
    }
}