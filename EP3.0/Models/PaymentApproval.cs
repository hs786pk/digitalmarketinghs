using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EarnPlatform.Models;

public class PaymentApproval
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public UserAccount User { get; set; } = default!;

    [Required]
    public int PackageId { get; set; }

    [ForeignKey(nameof(PackageId))]
    public Package Package { get; set; } = default!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required, MaxLength(50)]
    public string AccountNumber { get; set; } = default!;

    [Required, MaxLength(150)]
    public string AccountName { get; set; } = default!;

    [Required, MaxLength(100)]
    public string TransactionId { get; set; } = default!;

    // Nullable: the screenshot is deleted from disk (and this cleared) once an
    // admin approves or rejects the request, to keep storage usage down. It is
    // only ever non-null while the request is Pending.
    [MaxLength(300)]
    public string? PaymentScreenshotPath { get; set; }

    public PaymentApprovalStatus Status { get; set; } = PaymentApprovalStatus.Pending;

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReviewedAt { get; set; }

    [MaxLength(1000)]
    public string? AdminRemarks { get; set; }
}