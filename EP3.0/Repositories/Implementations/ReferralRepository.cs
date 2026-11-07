using EarnPlatform.Data;
using EarnPlatform.Models;
using EarnPlatform.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EarnPlatform.Repositories.Implementations;

public class ReferralRepository : GenericRepository<Referral>, IReferralRepository
{
    public ReferralRepository(IDbContextFactory<ApplicationDbContext> factory) : base(factory) { }

    public async Task<int> CountDirectReferralsAsync(int referrerUserId, CancellationToken cancellationToken = default)
    {
        await using var context = await Factory.CreateDbContextAsync(cancellationToken);
        return await context.Referrals.CountAsync(
            r => r.ReferrerUserId == referrerUserId && r.Status != ReferralStatus.Invalid,
            cancellationToken);
    }

    public async Task<int> CountSuccessfulReferralsAsync(int referrerUserId, CancellationToken cancellationToken = default)
    {
        await using var context = await Factory.CreateDbContextAsync(cancellationToken);
        return await context.Referrals.CountAsync(
            r => r.ReferrerUserId == referrerUserId && r.Status == ReferralStatus.Successful,
            cancellationToken);
    }
}