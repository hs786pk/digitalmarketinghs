using EarnPlatform.Models;

namespace EarnPlatform.Repositories.Interfaces;

public interface IReferralRepository : IGenericRepository<Referral>
{
    Task<int> CountDirectReferralsAsync(int referrerUserId, CancellationToken cancellationToken = default);
    Task<int> CountSuccessfulReferralsAsync(int referrerUserId, CancellationToken cancellationToken = default);
}