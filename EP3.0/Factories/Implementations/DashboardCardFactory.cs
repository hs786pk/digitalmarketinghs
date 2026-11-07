using EarnPlatform.Factories.Interfaces;

namespace EarnPlatform.Factories.Implementations;

/// <summary>
/// Factory Pattern: assembles the set of dashboard cards shown to each role.
/// Keeping this in a factory (rather than hard-coded markup in the Razor page)
/// means the card list can later be driven by feature flags, roles, or admin
/// configuration without touching any .razor file.
/// </summary>
public class DashboardCardFactory : IDashboardCardFactory
{
    public List<DashboardCardViewModel> CreateUserDashboardCards()
    {
        return new List<DashboardCardViewModel>
        {
            new("Withdraw", "Request a payout to JazzCash or Easypaisa", "bi bi-cash-coin", "/user/withdraw"),
            new("Team", "View your referrals and network stats", "bi bi-people-fill", "/user/team"),
            new("Guide", "Learn how the platform works", "bi bi-book-half", "/user/guide"),
            new("Official Channel", "Join our official WhatsApp channel", "bi bi-broadcast", "/user/channel"),
            new("Work", "Complete tasks and earn rewards", "bi bi-briefcase-fill", "/user/work"),
            new("Salary", "View your salary program details", "bi bi-wallet2", "/user/salary"),
            new("About Us", "Learn more about the platform", "bi bi-info-circle-fill", "/user/about"),
            new("Contact Us", "Reach our support team", "bi bi-headset", "/user/contact"),
            new("FBR", "View FBR approval details", "bi bi-patch-check-fill", "/user/fbr"),
            new("Wallet", "View balances and transaction history", "bi bi-wallet-fill", "/user/wallet"),
        };
    }

    public List<DashboardCardViewModel> CreateAdminDashboardCards(AdminDashboardStats stats)
    {
        return new List<DashboardCardViewModel>
        {
            new("Total Users", stats.TotalUsers.ToString(), "bi bi-people-fill", "/admin/users"),
            new("Pending Approvals", stats.PendingAccountApprovals.ToString(), "bi bi-hourglass-split", "/admin/account-approvals",
                stats.PendingAccountApprovals > 0 ? stats.PendingAccountApprovals.ToString() : null),
            new("Approved Users", stats.ApprovedUsers.ToString(), "bi bi-person-check-fill", "/admin/users"),
            new("Pending Withdrawals", stats.PendingWithdrawals.ToString(), "bi bi-cash-stack", "/admin/withdrawals",
                stats.PendingWithdrawals > 0 ? stats.PendingWithdrawals.ToString() : null),
            new("Total Withdrawals", $"PKR {stats.TotalWithdrawals:N0}", "bi bi-bank", "/admin/withdrawals"),
            new("Total Wallet Balance", $"PKR {stats.TotalWalletBalance:N0}", "bi bi-graph-up-arrow", "/admin/users"),
            new("Active Packages", stats.ActivePackages.ToString(), "bi bi-box-seam-fill", "/admin/packages"),
            new("Today's Completions", stats.TodaysCompletions.ToString(), "bi bi-check2-circle", "/admin/tasks"),
        };
    }
}