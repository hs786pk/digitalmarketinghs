using EarnPlatform.Models;

namespace EarnPlatform.Services.Interfaces;

public interface IWorkTaskService
{
    Task<List<WorkTask>> GetActiveTasksForUserAsync(CancellationToken cancellationToken = default);
    Task<int> GetRemainingDailyTasksAsync(int userId, CancellationToken cancellationToken = default);
    Task<WorkTaskCompletion> StartTaskAsync(int userId, int workTaskId, CancellationToken cancellationToken = default);
    Task<WorkTaskCompletion> CompleteTaskAsync(int userId, int completionId, CancellationToken cancellationToken = default);

    Task<List<WorkTask>> GetAllTasksAsync(CancellationToken cancellationToken = default);
    Task<WorkTask> CreateTaskAsync(string title, string? description, string adUrl, decimal rewardAmount, CancellationToken cancellationToken = default);
    Task UpdateTaskAsync(int id, string title, string? description, string adUrl, decimal rewardAmount, bool isActive, CancellationToken cancellationToken = default);
}