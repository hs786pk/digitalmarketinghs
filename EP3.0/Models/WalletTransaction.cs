using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EarnPlatform.Models;

public class WalletTransaction
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public UserAccount User { get; set; } = default!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public WalletTransactionType TransactionType { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public WalletTransactionStatus Status { get; set; } = WalletTransactionStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ApprovedAt { get; set; }
}