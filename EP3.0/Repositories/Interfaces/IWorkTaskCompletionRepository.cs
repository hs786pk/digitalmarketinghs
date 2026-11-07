using EarnPlatform.Models;

namespace EarnPlatform.Repositories.Interfaces;

public interface IWorkTaskCompletionRepository : IGenericRepository<WorkTaskCompletion>
{
    Task<int> CountTodaysCompletionsAsync(int userId, CancellationToken cancellationToken = default);
    Task<int> CountAllTodaysCompletionsAsync(CancellationToken cancellationToken = default);
    Task<WorkTaskCompletion?> GetWithTaskAsync(int completionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts this user's currently ACTIVE "Started" sessions today (i.e. Started
    /// and not yet past the task-session lifetime). Stale/expired Started rows are
    /// never counted as active. Used to enforce the daily task limit consistently.
    /// </summary>
    Task<int> CountActiveStartedTodayAsync(int userId, DateTime now, DateTime todayStart, DateTime tomorrowStart, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the user's currently active (Started, not yet expired) session for a
    /// specific task, or null if none exists. Used to decide whether a new session
    /// may be started for the same task.
    /// </summary>
    Task<WorkTaskCompletion?> GetActiveSessionAsync(int userId, int workTaskId, DateTime now, CancellationToken cancellationToken = default);

    /// <summary>
    /// Transitions any stale "Started" sessions for this user (StartedAt older than
    /// the task-session lifetime) to "Expired". Never touches Completed/Credited or
    /// already-Expired rows, and is a no-op (safe to call repeatedly) once nothing
    /// is stale. Returns the number of rows updated.
    /// </summary>
    Task<int> ExpireStaleSessionsAsync(int userId, DateTime now, CancellationToken cancellationToken = default);
}
