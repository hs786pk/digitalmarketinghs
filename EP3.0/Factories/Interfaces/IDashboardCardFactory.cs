namespace EarnPlatform.Factories.Interfaces;

public record DashboardCardViewModel(string Title, string Description, string IconCssClass, string NavigateUrl, string? BadgeText = null);

public interface IDashboardCardFactory
{
    List<DashboardCardViewModel> CreateUserDashboardCards();
    List<DashboardCardViewModel> CreateAdminDashboardCards(AdminDashboardStats stats);
}

public record AdminDashboardStats(
    int TotalUsers,
    int PendingAccountApprovals,
    int ApprovedUsers,
    int PendingWithdrawals,
    decimal TotalWithdrawals,
    decimal TotalWalletBalance,
    int ActivePackages,
    int TodaysCompletions);