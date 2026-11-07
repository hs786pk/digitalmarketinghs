using System.Data;
using EarnPlatform.Common.Exceptions;
using EarnPlatform.Data;
using EarnPlatform.Factories.Interfaces;
using EarnPlatform.Models;
using EarnPlatform.Repositories.Interfaces;
using EarnPlatform.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EarnPlatform.Services.Implementations;

public class WorkTaskService : IWorkTaskService
{
    private static readonly TimeSpan TaskSessionLifetime = Common.BusinessRules.TaskSessionLifetime;

    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly IWorkTaskRepository _workTaskRepository;
    private readonly IWorkTaskCompletionRepository _completionRepository;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IWorkTaskFactory _workTaskFactory;
    private readonly IWalletService _walletService;
    private readonly IBusinessClock _clock;

    public WorkTaskService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        IWorkTaskRepository workTaskRepository,
        IWorkTaskCompletionRepository completionRepository,
        IUserAccountRepository userAccountRepository,
        IWorkTaskFactory workTaskFactory,
        IWalletService walletService,
        IBusinessClock clock)
    {
        _dbFactory = dbFactory;
        _workTaskRepository = workTaskRepository;
        _completionRepository = completionRepository;
        _userAccountRepository = userAccountRepository;
        _workTaskFactory = workTaskFactory;
        _walletService = walletService;
        _clock = clock;
    }

    public Task<List<WorkTask>> GetActiveTasksForUserAsync(CancellationToken cancellationToken = default) =>
        _workTaskRepository.GetActiveTasksAsync(cancellationToken);

    public async Task<int> GetRemainingDailyTasksAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _userAccountRepository.GetWithPackageAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");
        if (user.AccountStatus != AccountStatus.Active || user.SelectedPackage == null)
            return 0;

        var now = _clock.UtcNow;
        var today = _clock.TodayUtcStart;
        var tomorrow = _clock.TomorrowUtcStart;

        await _completionRepository.ExpireStaleSessionsAsync(userId, now, cancellationToken);

        var completedToday = await _completionRepository.CountTodaysCompletionsAsync(userId, cancellationToken);
        var activeStartedToday = await _completionRepository.CountActiveStartedTodayAsync(
            userId, now, today, tomorrow, cancellationToken);
        return Math.Max(0, user.SelectedPackage.DailyAdLimit - completedToday - activeStartedToday);
    }

    public async Task<WorkTaskCompletion> StartTaskAsync(
        int userId, int workTaskId, CancellationToken cancellationToken = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var strategy = context.Database.CreateExecutionStrategy();
        WorkTaskCompletion? result = null;

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, cancellationToken);

            var user = await context.UserAccounts
                .FromSqlInterpolated($"SELECT * FROM [UserAccounts] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {userId}")
                .Include(u => u.SelectedPackage)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("User not found.");

            if (user.AccountStatus != AccountStatus.Active)
                throw new ForbiddenException("Only approved, active accounts can perform work tasks.");
            if (user.SelectedPackage == null)
                throw new ForbiddenException("You do not have an active package.");

            var task = await context.WorkTasks.FirstOrDefaultAsync(t => t.Id == workTaskId, cancellationToken)
                ?? throw new NotFoundException("Task not found.");
            if (!task.IsActive)
                throw new ValidationException("This task is no longer available.");

            var now = _clock.UtcNow;
            var today = _clock.TodayUtcStart;
            var tomorrow = _clock.TomorrowUtcStart;
            var cutoff = now - TaskSessionLifetime;

            await context.WorkTaskCompletions
                .Where(c => c.UserId == userId && c.Status == WorkTaskCompletionStatus.Started
                    && (c.StartedAt < cutoff || c.StartedAt > now))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.Status, WorkTaskCompletionStatus.Expired)
                    .SetProperty(c => c.CompletedAt, now), cancellationToken);

            var creditedToday = await context.WorkTaskCompletions.CountAsync(c =>
                c.UserId == userId &&
                c.Status == WorkTaskCompletionStatus.Credited &&
                c.CompletedAt >= today && c.CompletedAt < tomorrow, cancellationToken);

            var activeStartedToday = await context.WorkTaskCompletions.CountAsync(c =>
                c.UserId == userId &&
                c.Status == WorkTaskCompletionStatus.Started &&
                c.StartedAt >= today && c.StartedAt < tomorrow &&
                c.StartedAt <= now &&
                c.StartedAt >= cutoff, cancellationToken);

            if (creditedToday + activeStartedToday >= user.SelectedPackage.DailyAdLimit)
                throw new ValidationException("You have reached your daily task limit for your package.");

            if (await context.WorkTaskCompletions.AnyAsync(c =>
                c.UserId == userId && c.WorkTaskId == workTaskId &&
                c.Status == WorkTaskCompletionStatus.Credited &&
                c.CompletedAt >= today && c.CompletedAt < tomorrow, cancellationToken))
                throw new ConflictException("You have already completed this task today.");

            if (await context.WorkTaskCompletions.AnyAsync(c =>
                c.UserId == userId && c.WorkTaskId == workTaskId &&
                c.Status == WorkTaskCompletionStatus.Started &&
                c.StartedAt <= now && c.StartedAt >= cutoff, cancellationToken))
                throw new ConflictException("You already have an active session for this task.");

            result = new WorkTaskCompletion
            {
                UserId = userId,
                WorkTaskId = workTaskId,
                RewardAmount = task.RewardAmount,
                Status = WorkTaskCompletionStatus.Started,
                StartedAt = now
            };
            await context.WorkTaskCompletions.AddAsync(result, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });

        return result!;
    }

    public async Task<WorkTaskCompletion> CompleteTaskAsync(
        int userId, int completionId, CancellationToken cancellationToken = default)
    {
        await _walletService.CompleteWorkTaskAsync(userId, completionId, cancellationToken);
        return await _completionRepository.GetByIdAsync(completionId, cancellationToken)
            ?? throw new NotFoundException("Task session not found after completion.");
    }

    public Task<List<WorkTask>> GetAllTasksAsync(CancellationToken cancellationToken = default) =>
        _workTaskRepository.GetAllAsync(cancellationToken);

    public async Task<WorkTask> CreateTaskAsync(
        string title, string? description, string adUrl, decimal rewardAmount,
        CancellationToken cancellationToken = default)
    {
        ValidateTask(title, adUrl, rewardAmount);
        var task = _workTaskFactory.CreateWorkTask(title.Trim(), description?.Trim(), adUrl.Trim(), rewardAmount);
        await _workTaskRepository.AddAsync(task, cancellationToken);
        return task;
    }

    public async Task UpdateTaskAsync(
        int id, string title, string? description, string adUrl, decimal rewardAmount, bool isActive,
        CancellationToken cancellationToken = default)
    {
        ValidateTask(title, adUrl, rewardAmount);
        var task = await _workTaskRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Task not found.");
        task.Title = title.Trim();
        task.Description = description?.Trim();
        task.AdUrl = adUrl.Trim();
        task.RewardAmount = rewardAmount;
        task.IsActive = isActive;
        await _workTaskRepository.UpdateAsync(task, cancellationToken);
    }

    private static void ValidateTask(string title, string adUrl, decimal rewardAmount)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 150)
            throw new ValidationException("Task title is required and must be 150 characters or fewer.");
        if (rewardAmount <= 0)
            throw new ValidationException("Reward amount must be positive.");
        if (!Uri.TryCreate(adUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            throw new ValidationException("A valid HTTP/HTTPS task URL is required.");
    }

    public static bool IsSessionExpired(DateTime startedAt, DateTime now) =>
        startedAt > now || now - startedAt > TaskSessionLifetime;

    public static bool IsActiveSession(WorkTaskCompletion completion, DateTime now) =>
        completion.Status == WorkTaskCompletionStatus.Started
        && Common.BusinessRules.IsActiveSession(completion.StartedAt, now);
}