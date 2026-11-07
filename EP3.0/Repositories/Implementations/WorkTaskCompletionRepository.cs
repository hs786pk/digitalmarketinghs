using EarnPlatform.Data;
using EarnPlatform.Models;
using EarnPlatform.Repositories.Interfaces;
using EarnPlatform.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EarnPlatform.Repositories.Implementations;

public class WorkTaskCompletionRepository : GenericRepository<WorkTaskCompletion>, IWorkTaskCompletionRepository
{
    private readonly IBusinessClock _clock;

    public WorkTaskCompletionRepository(IDbContextFactory<ApplicationDbContext> factory, IBusinessClock clock)
        : base(factory) => _clock = clock;

    public async Task<int> CountTodaysCompletionsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var today = _clock.TodayUtcStart;
        var tomorrow = _clock.TomorrowUtcStart;
        await using var context = await Factory.CreateDbContextAsync(cancellationToken);
        return await context.WorkTaskCompletions.CountAsync(c =>
            c.UserId == userId &&
            c.Status == WorkTaskCompletionStatus.Credited &&
            c.CompletedAt >= today && c.CompletedAt < tomorrow, cancellationToken);
    }

    public async Task<int> CountAllTodaysCompletionsAsync(CancellationToken cancellationToken = default)
    {
        var today = _clock.TodayUtcStart;
        var tomorrow = _clock.TomorrowUtcStart;
        await using var context = await Factory.CreateDbContextAsync(cancellationToken);
        return await context.WorkTaskCompletions.CountAsync(c =>
            c.Status == WorkTaskCompletionStatus.Credited &&
            c.CompletedAt >= today && c.CompletedAt < tomorrow, cancellationToken);
    }

    public async Task<WorkTaskCompletion?> GetWithTaskAsync(int completionId, CancellationToken cancellationToken = default)
    {
        await using var context = await Factory.CreateDbContextAsync(cancellationToken);
        return await context.WorkTaskCompletions
            .Include(c => c.WorkTask)
            .FirstOrDefaultAsync(c => c.Id == completionId, cancellationToken);
    }

    public async Task<int> CountActiveStartedTodayAsync(
        int userId, DateTime now, DateTime todayStart, DateTime tomorrowStart,
        CancellationToken cancellationToken = default)
    {
        var cutoff = now - EarnPlatform.Common.BusinessRules.TaskSessionLifetime;
        await using var context = await Factory.CreateDbContextAsync(cancellationToken);
        return await context.WorkTaskCompletions.CountAsync(c =>
            c.UserId == userId &&
            c.Status == WorkTaskCompletionStatus.Started &&
            c.StartedAt >= todayStart && c.StartedAt < tomorrowStart &&
            c.StartedAt <= now &&
            c.StartedAt >= cutoff, cancellationToken);
    }

    public async Task<WorkTaskCompletion?> GetActiveSessionAsync(
        int userId, int workTaskId, DateTime now,
        CancellationToken cancellationToken = default)
    {
        var cutoff = now - EarnPlatform.Common.BusinessRules.TaskSessionLifetime;
        await using var context = await Factory.CreateDbContextAsync(cancellationToken);
        return await context.WorkTaskCompletions.FirstOrDefaultAsync(c =>
            c.UserId == userId && c.WorkTaskId == workTaskId &&
            c.Status == WorkTaskCompletionStatus.Started &&
            c.StartedAt <= now &&
            c.StartedAt >= cutoff, cancellationToken);
    }

    public async Task<int> ExpireStaleSessionsAsync(int userId, DateTime now, CancellationToken cancellationToken = default)
    {
        var cutoff = now - EarnPlatform.Common.BusinessRules.TaskSessionLifetime;
        await using var context = await Factory.CreateDbContextAsync(cancellationToken);
        return await context.WorkTaskCompletions
            .Where(c => c.UserId == userId && c.Status == WorkTaskCompletionStatus.Started
                && (c.StartedAt < cutoff || c.StartedAt > now))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(c => c.Status, WorkTaskCompletionStatus.Expired)
                .SetProperty(c => c.CompletedAt, now), cancellationToken);
    }
}