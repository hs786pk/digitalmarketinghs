using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EarnPlatform.Models;

public class Referral
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ReferrerUserId { get; set; }

    [ForeignKey(nameof(ReferrerUserId))]
    public UserAccount ReferrerUser { get; set; } = default!;

    [Required]
    public int ReferredUserId { get; set; }

    [ForeignKey(nameof(ReferredUserId))]
    public UserAccount ReferredUser { get; set; } = default!;

    [Required, MaxLength(20)]
    public string ReferralCode { get; set; } = default!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ReferralStatus Status { get; set; } = ReferralStatus.Active;
}