using EarnPlatform.Data;
using EarnPlatform.Models;
using EarnPlatform.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EarnPlatform.Repositories.Implementations;

public class PaymentApprovalRepository : GenericRepository<PaymentApproval>, IPaymentApprovalRepository
{
    public PaymentApprovalRepository(IDbContextFactory<ApplicationDbContext> factory) : base(factory) { }

    public async Task<List<PaymentApproval>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await Factory.CreateDbContextAsync(cancellationToken);
        return await context.PaymentApprovals
            .AsNoTracking()
            .Where(p => p.Status == PaymentApprovalStatus.Pending)
            .Include(p => p.User)
            .Include(p => p.Package)
            .OrderBy(p => p.SubmittedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<PaymentApproval?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await Factory.CreateDbContextAsync(cancellationToken);
        return await context.PaymentApprovals
            .Include(p => p.User)
            .Include(p => p.Package)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }
}