using EarnPlatform.Data;
using EarnPlatform.Models;
using EarnPlatform.Models.Dtos;
using EarnPlatform.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EarnPlatform.Services.Implementations;

public class UserDashboardService : IUserDashboardService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly ICurrentUserService _currentUser;
    private readonly IWorkTaskService _workTaskService;

    public UserDashboardService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        ICurrentUserService currentUser,
        IWorkTaskService workTaskService)
    {
        _dbFactory = dbFactory;
        _currentUser = currentUser;
        _workTaskService = workTaskService;
    }

    public async Task<UserDashboardDto?> GetAsync(CancellationToken cancellationToken = default)
    {
        // Circuit-cached identity (no repeated DB hit for ApplicationUserId)
        var user = await _currentUser.GetCurrentUserAccountAsync(cancellationToken);
        if (user == null) return null;

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        // Projection — only the columns we need
        var snapshot = await db.UserAccounts
            .AsNoTracking()
            .Where(u => u.Id == user.Id)
            .Select(u => new
            {
                u.Id,
                u.FirstName,
                u.LastName,
                u.ReferralCode,
                u.AccountLevel,
                Status = u.AccountStatus,
                u.SelectedPackageId,
                PackageName = u.SelectedPackage != null ? u.SelectedPackage.Name : null,
                DailyAdLimit = u.SelectedPackage != null ? u.SelectedPackage.DailyAdLimit : 0,
                Balance = u.Wallet != null ? u.Wallet.Balance : 0m,
                CurrentEarning = u.Wallet != null ? u.Wallet.CurrentEarning : 0m,
                ReferralEarning = u.Wallet != null ? u.Wallet.ReferralEarning : 0m,
                PendingEarning = u.Wallet != null ? u.Wallet.PendingEarning : 0m
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (snapshot == null) return null;

        var remaining = 0;
        if (snapshot.Status == AccountStatus.Active && snapshot.SelectedPackageId != null)
            remaining = await _workTaskService.GetRemainingDailyTasksAsync(snapshot.Id, cancellationToken);

        return new UserDashboardDto
        {
            UserId = snapshot.Id,
            FirstName = snapshot.FirstName,
            LastName = snapshot.LastName,
            ReferralCode = snapshot.ReferralCode,
            AccountLevel = snapshot.AccountLevel,
            AccountStatus = snapshot.Status.ToString(),
            SelectedPackageId = snapshot.SelectedPackageId,
            PackageName = snapshot.PackageName,
            DailyAdLimit = snapshot.DailyAdLimit,
            Balance = snapshot.Balance,
            CurrentEarning = snapshot.CurrentEarning,
            ReferralEarning = snapshot.ReferralEarning,
            PendingEarning = snapshot.PendingEarning,
            RemainingDailyTasks = remaining
        };
    }
}