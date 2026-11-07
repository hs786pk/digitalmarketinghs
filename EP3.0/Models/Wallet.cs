using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EarnPlatform.Models;

public class Wallet
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public UserAccount User { get; set; } = default!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal CurrentEarning { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ReferralEarning { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal StaffEarning { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ApprovedEarning { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PendingEarning { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal RejectedEarning { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Balance { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Note: intentionally NOT using an optimistic concurrency token (RowVersion)
    // here. Blazor Server's DbContext is scoped per circuit (i.e. per browser
    // session, not per request), so tracked entities persist across many
    // operations — a RowVersion token in that environment produces false-positive
    // concurrency conflicts even when nothing is actually wrong. Real safety for
    // wallet mutations comes from the explicit DB transaction wrapping every
    // change in WalletService (BeginTransactionAsync), which serializes writes
    // correctly without this pitfall.
}