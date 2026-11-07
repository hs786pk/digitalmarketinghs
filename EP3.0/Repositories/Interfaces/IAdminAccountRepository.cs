using EarnPlatform.Models;

namespace EarnPlatform.Repositories.Interfaces;

public interface IAdminAccountRepository : IGenericRepository<AdminAccount>
{
    Task<AdminAccount?> GetByApplicationUserIdAsync(string applicationUserId, CancellationToken cancellationToken = default);
}