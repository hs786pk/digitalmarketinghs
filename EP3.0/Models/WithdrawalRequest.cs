using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EarnPlatform.Models;

public class WithdrawalRequest
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public UserAccount User { get; set; } = default!;

    public PaymentMethod PaymentMethod { get; set; }

    [Required, MaxLength(50)]
    public string AccountNumber { get; set; } = default!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public WithdrawalStatus Status { get; set; } = WithdrawalStatus.Pending;

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedAt { get; set; }

    [MaxLength(1000)]
    public string? AdminRemarks { get; set; }

    // Links back to the WalletTransaction that reserved/reversed funds for this request
    public int? RelatedWalletTransactionId { get; set; }

    [ForeignKey(nameof(RelatedWalletTransactionId))]
    public WalletTransaction? RelatedWalletTransaction { get; set; }
}