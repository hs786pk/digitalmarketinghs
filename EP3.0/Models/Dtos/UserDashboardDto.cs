namespace EarnPlatform.Models.Dtos;

public sealed class UserDashboardDto
{
    public int UserId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string ReferralCode { get; init; } = string.Empty;
    public int AccountLevel { get; init; }
    public string AccountStatus { get; init; } = string.Empty;
    public int? SelectedPackageId { get; init; }
    public string? PackageName { get; init; }
    public int DailyAdLimit { get; init; }

    public decimal Balance { get; init; }
    public decimal CurrentEarning { get; init; }
    public decimal ReferralEarning { get; init; }
    public decimal PendingEarning { get; init; }

    public int RemainingDailyTasks { get; init; }
}