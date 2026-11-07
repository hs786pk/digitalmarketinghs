using EarnPlatform.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EarnPlatform.Models;

public class UserAccount
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string ApplicationUserId { get; set; } = default!;

    [ForeignKey(nameof(ApplicationUserId))]
    public ApplicationUser ApplicationUser { get; set; } = default!;

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = default!;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = default!;

    [Required, MaxLength(256)]
    public string Email { get; set; } = default!;

    [Required, MaxLength(20)]
    public string ReferralCode { get; set; } = default!;

    public int? ReferredByUserId { get; set; }

    [ForeignKey(nameof(ReferredByUserId))]
    public UserAccount? ReferredByUser { get; set; }

    public int? SelectedPackageId { get; set; }

    [ForeignKey(nameof(SelectedPackageId))]
    public Package? SelectedPackage { get; set; }

    public int AccountLevel { get; set; } = 0;

    /// <summary>
    /// Count of this user's referred users who have been approved (i.e. became
    /// active earners). Every N successful referrals (see
    /// BusinessRules.ReferralsPerLevelUp) auto-increments AccountLevel by 1.
    /// </summary>
    public int SuccessfulReferralCount { get; set; } = 0;

    public AccountStatus AccountStatus { get; set; } = AccountStatus.PendingApproval;


    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ApprovedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public Wallet? Wallet { get; set; }
    public ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();
    public ICollection<Referral> ReferralsMade { get; set; } = new List<Referral>();
    public ICollection<PaymentApproval> PaymentApprovals { get; set; } = new List<PaymentApproval>();
    public ICollection<WithdrawalRequest> WithdrawalRequests { get; set; } = new List<WithdrawalRequest>();
    public ICollection<WorkTaskCompletion> WorkTaskCompletions { get; set; } = new List<WorkTaskCompletion>();
    public ICollection<UserAccount> ReferredUsers { get; set; } = new List<UserAccount>();
}