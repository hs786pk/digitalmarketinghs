using EarnPlatform.Data;
using EarnPlatform.Models;
using EarnPlatform.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EarnPlatform.Repositories.Implementations;

public class WithdrawalRequestRepository : GenericRepository<WithdrawalRequest>, IWithdrawalRequestRepository
{
    public WithdrawalRequestRepository(IDbContextFactory<ApplicationDbContext> factory) : base(factory) { }

    public async Task<List<WithdrawalRequest>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await Factory.CreateDbContextAsync(cancellationToken);
        return await context.WithdrawalRequests
            .AsNoTracking()
            .Where(w => w.Status == WithdrawalStatus.Pending)
            .Include(w => w.User)
            .OrderByDescending(w => w.RequestedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<WithdrawalRequest>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        await using var context = await Factory.CreateDbContextAsync(cancellationToken);
        return await context.WithdrawalRequests
            .AsNoTracking()
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.RequestedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<WithdrawalRequest>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await Factory.CreateDbContextAsync(cancellationToken);
        return await context.WithdrawalRequests
            .AsNoTracking()
            .Include(w => w.User)
            .OrderByDescending(w => w.RequestedAt)
            .ToListAsync(cancellationToken);
    }
}