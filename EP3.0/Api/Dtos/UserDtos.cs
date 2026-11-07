namespace EarnPlatform.Api.Dtos;

public class ProfileDto
{
    public int UserAccountId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string ReferralCode { get; set; } = string.Empty;
    public string ReferralLink { get; set; } = string.Empty;
    public string AccountStatus { get; set; } = string.Empty;
    public int AccountLevel { get; set; }
    public int SuccessfulReferralCount { get; set; }
    public int? SelectedPackageId { get; set; }
    public string? PackageName { get; set; }
    public int? DailyAdLimit { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class DashboardDto
{
    public string FirstName { get; set; } = string.Empty;
    public string AccountStatus { get; set; } = string.Empty;
    public string ReferralCode { get; set; } = string.Empty;
    public string ReferralLink { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public decimal CurrentEarning { get; set; }
    public decimal ReferralEarning { get; set; }
    public decimal PendingEarning { get; set; }
    public decimal ApprovedEarning { get; set; }
    public int AccountLevel { get; set; }
    public int? SelectedPackageId { get; set; }
    public string? PackageName { get; set; }
}

public class WalletDto
{
    public decimal Balance { get; set; }
    public decimal CurrentEarning { get; set; }
    public decimal ReferralEarning { get; set; }
    public decimal StaffEarning { get; set; }
    public decimal ApprovedEarning { get; set; }
    public decimal PendingEarning { get; set; }
    public decimal RejectedEarning { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<WalletTransactionDto> Transactions { get; set; } = new();
    public List<WithdrawalDto> Withdrawals { get; set; } = new();
}

public class WalletTransactionDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
}

public class WithdrawalDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}