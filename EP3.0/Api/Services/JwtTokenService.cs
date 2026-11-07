using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EarnPlatform.Data;
using EarnPlatform.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace EarnPlatform.Api.Services;

public class JwtTokenService
{
    private readonly IConfiguration _config;
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public JwtTokenService(IConfiguration config, IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        _config = config;
        _dbFactory = dbFactory;
    }

    public (string AccessToken, DateTime AccessExpiresAtUtc) CreateAccessToken(
        ApplicationUser user,
        UserAccount? userAccount,
        IList<string> roles)
    {
        var key = _config["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        var issuer = _config["Jwt:Issuer"] ?? "https://digital-marketingpk.com";
        var audience = _config["Jwt:Audience"] ?? "EarnPlatform.Mobile";
        var expireMinutes = _config.GetValue("Jwt:AccessTokenMinutes", 30);

        var expires = DateTime.UtcNow.AddMinutes(expireMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (userAccount != null)
        {
            claims.Add(new Claim("user_account_id", userAccount.Id.ToString()));
            claims.Add(new Claim("account_status", userAccount.AccountStatus.ToString()));
            claims.Add(new Claim(ClaimTypes.GivenName, userAccount.FirstName));
            claims.Add(new Claim(ClaimTypes.Surname, userAccount.LastName));
        }

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    public async Task<(string RawRefreshToken, RefreshToken Entity)> IssueRefreshTokenAsync(
        string applicationUserId,
        string? deviceName,
        string? ip,
        CancellationToken cancellationToken = default)
    {
        var days = _config.GetValue("Jwt:RefreshTokenDays", 60);
        var raw = GenerateSecureToken();
        var entity = new RefreshToken
        {
            ApplicationUserId = applicationUserId,
            TokenHash = HashToken(raw),
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(days),
            DeviceName = Truncate(deviceName, 200),
            CreatedByIp = Truncate(ip, 45)
        };

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await db.RefreshTokens.AddAsync(entity, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return (raw, entity);
    }

    /// <summary>
    /// Rotates refresh token: validates old, revokes it, issues new.
    /// Reuse of an already-revoked token revokes the whole family for that user (theft detection).
    /// </summary>
    public async Task<(string RawRefreshToken, RefreshToken Entity)?> RotateRefreshTokenAsync(
        string rawRefreshToken,
        string? deviceName,
        string? ip,
        CancellationToken cancellationToken = default)
    {
        var hash = HashToken(rawRefreshToken);
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var existing = await db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (existing == null)
            return null;

        // Token reuse after revoke → possible theft: kill all sessions for this user
        if (existing.RevokedAtUtc != null)
        {
            await RevokeAllForUserAsync(db, existing.ApplicationUserId, cancellationToken);
            return null;
        }

        if (existing.ExpiresAtUtc <= DateTime.UtcNow)
            return null;

        existing.RevokedAtUtc = DateTime.UtcNow;

        var days = _config.GetValue("Jwt:RefreshTokenDays", 60);
        var raw = GenerateSecureToken();
        var replacement = new RefreshToken
        {
            ApplicationUserId = existing.ApplicationUserId,
            TokenHash = HashToken(raw),
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(days),
            DeviceName = Truncate(deviceName, 200) ?? existing.DeviceName,
            CreatedByIp = Truncate(ip, 45)
        };
        existing.ReplacedByTokenHash = replacement.TokenHash;

        await db.RefreshTokens.AddAsync(replacement, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return (raw, replacement);
    }

    public async Task<bool> RevokeRefreshTokenAsync(string rawRefreshToken, CancellationToken cancellationToken = default)
    {
        var hash = HashToken(rawRefreshToken);
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var existing = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (existing == null || existing.RevokedAtUtc != null)
            return false;

        existing.RevokedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task RevokeAllForUserAsync(string applicationUserId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await RevokeAllForUserAsync(db, applicationUserId, cancellationToken);
    }

    private static async Task RevokeAllForUserAsync(
        ApplicationDbContext db, string applicationUserId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        await db.RefreshTokens
            .Where(t => t.ApplicationUserId == applicationUserId && t.RevokedAtUtc == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAtUtc, now), cancellationToken);
    }

    public static string HashToken(string raw)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes);
    }

    private static string GenerateSecureToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    private static string? Truncate(string? value, int max) =>
        string.IsNullOrEmpty(value) ? value : (value.Length <= max ? value : value[..max]);
}