using EarnPlatform.Models;

namespace EarnPlatform.Repositories.Interfaces;

public interface IUserAccountRepository : IGenericRepository<UserAccount>
{
    Task<UserAccount?> GetByApplicationUserIdAsync(string applicationUserId, CancellationToken cancellationToken = default);
    Task<UserAccount?> GetByReferralCodeAsync(string referralCode, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ReferralCodeExistsAsync(string referralCode, CancellationToken cancellationToken = default);
    Task<List<UserAccount>> GetReferredUsersAsync(int referrerUserId, CancellationToken cancellationToken = default);
    Task<UserAccount?> GetWithPackageAsync(int userId, CancellationToken cancellationToken = default);
    Task<List<UserAccount>> GetAllWithPackageAsync(CancellationToken cancellationToken = default);
}
