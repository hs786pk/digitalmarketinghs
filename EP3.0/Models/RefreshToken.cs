using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EarnPlatform.Models;

public class RefreshToken
{
    [Key]
    public long Id { get; set; }

    [Required, MaxLength(450)]
    public string ApplicationUserId { get; set; } = default!;

    /// <summary>SHA-256 hash of the raw token (never store plaintext).</summary>
    [Required, MaxLength(64)]
    public string TokenHash { get; set; } = default!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    /// <summary>Hash of the token that replaced this one (rotation chain).</summary>
    [MaxLength(64)]
    public string? ReplacedByTokenHash { get; set; }

    [MaxLength(200)]
    public string? DeviceName { get; set; }

    [MaxLength(45)]
    public string? CreatedByIp { get; set; }

    [NotMapped]
    public bool IsActive => RevokedAtUtc == null && DateTime.UtcNow < ExpiresAtUtc;
}