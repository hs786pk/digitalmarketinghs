using EarnPlatform.Models;

namespace EarnPlatform.Repositories.Interfaces;

public interface IWorkTaskRepository : IGenericRepository<WorkTask>
{
    Task<List<WorkTask>> GetActiveTasksAsync(CancellationToken cancellationToken = default);
}