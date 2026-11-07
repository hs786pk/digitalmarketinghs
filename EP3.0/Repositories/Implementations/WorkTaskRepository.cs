using EarnPlatform.Data;
using EarnPlatform.Models;
using EarnPlatform.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EarnPlatform.Repositories.Implementations;

public class WorkTaskRepository : GenericRepository<WorkTask>, IWorkTaskRepository
{
    public WorkTaskRepository(IDbContextFactory<ApplicationDbContext> factory) : base(factory) { }

    public async Task<List<WorkTask>> GetActiveTasksAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await Factory.CreateDbContextAsync(cancellationToken);
        return await context.WorkTasks
            .AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Id)
            .Take(5)
            .ToListAsync(cancellationToken);
    }
}