using EarnPlatform.Data;
using EarnPlatform.Models;
using EarnPlatform.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EarnPlatform.Repositories.Implementations;

public class AdminAccountRepository : GenericRepository<AdminAccount>, IAdminAccountRepository
{
    public AdminAccountRepository(IDbContextFactory<ApplicationDbContext> factory) : base(factory) { }

    public async Task<AdminAccount?> GetByApplicationUserIdAsync(string applicationUserId, CancellationToken cancellationToken = default)
    {
        await using var context = await Factory.CreateDbContextAsync(cancellationToken);
        return await context.AdminAccounts
            .FirstOrDefaultAsync(a => a.ApplicationUserId == applicationUserId, cancellationToken);
    }
}