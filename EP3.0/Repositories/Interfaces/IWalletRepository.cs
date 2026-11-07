using EarnPlatform.Models;

namespace EarnPlatform.Repositories.Interfaces;

public interface IWalletRepository : IGenericRepository<Wallet>
{
    Task<Wallet?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalBalanceAsync(CancellationToken cancellationToken = default);
}
