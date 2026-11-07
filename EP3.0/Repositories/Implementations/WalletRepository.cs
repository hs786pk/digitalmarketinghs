using EarnPlatform.Data;
using EarnPlatform.Models;
using EarnPlatform.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EarnPlatform.Repositories.Implementations;

public class WalletRepository : GenericRepository<Wallet>, IWalletRepository
{
    public WalletRepository(IDbContextFactory<ApplicationDbContext> factory) : base(factory) { }

    public async Task<Wallet?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        await using var context = await Factory.CreateDbContextAsync(cancellationToken);
        return await context.Wallets
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);
    }

    public async Task<decimal> GetTotalBalanceAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await Factory.CreateDbContextAsync(cancellationToken);
        return await context.Wallets.SumAsync(w => w.Balance, cancellationToken);
    }
}