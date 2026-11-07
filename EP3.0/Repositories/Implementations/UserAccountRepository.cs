using EarnPlatform.Data;
using EarnPlatform.Models;
using EarnPlatform.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EarnPlatform.Repositories.Implementations;

public class UserAccountRepository : GenericRepository<UserAccount>, IUserAccountRepository
{
    public UserAccountRepository(IDbContextFactory<ApplicationDbContext> factory) : base(factory) { }

    public async Task<UserAccount?> GetByApplicationUserIdAsync(string applicationUserId, CancellationToken cancellationToken = default)
    {
        await using var context = await Factory.CreateDbContextAsync(cancellationToken);
        return await context.UserAccounts
            .AsNoTracking()
            .Include(u => u.SelectedPackage)
            .FirstOrDefaultAsync(u => u.ApplicationUserId == applicationUserId, cancellationToken);
    }

    public async Task<UserAccount?> GetByReferralCodeAsync(string referralCode, CancellationToken cancellationToken = default)
    {
        await using var context = await Factory.CreateDbContextAsync(cancellationToken);
        return await context.UserAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.ReferralCode == referralCode, cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        await using var context = await Factory.CreateDbContextAsync(cancellationToken);
        return await context.UserAccounts.AnyAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<bool> ReferralCodeExistsAsync(string referralCode, CancellationToken cancellationToken = default)
    {
        await using var context = await Factory.CreateDbContextAsync(cancellationToken);
        return await context.UserAccounts.AnyAsync(u => u.ReferralCode == referralCode, cancellationToken);
    }

    public async Task<List<UserAccount>> GetReferredUsersAsync(int referrerUserId, CancellationToken cancellationToken = default)
    {
        await using var context = await Factory.CreateDbContextAsync(cancellationToken);
        return await context.UserAccounts
            .AsNoTracking()
            .Where(u => u.ReferredByUserId == referrerUserId)
            .Include(u => u.SelectedPackage)
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserAccount?> GetWithPackageAsync(int userId, CancellationToken cancellationToken = default)
    {
        await using var context = await Factory.CreateDbContextAsync(cancellationToken);
        return await context.UserAccounts
            .AsNoTracking()
            .Include(u => u.SelectedPackage)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }

    public async Task<List<UserAccount>> GetAllWithPackageAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await Factory.CreateDbContextAsync(cancellationToken);
        return await context.UserAccounts
            .AsNoTracking()
            .Include(u => u.SelectedPackage)
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}