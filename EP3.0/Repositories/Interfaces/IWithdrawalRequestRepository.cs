using EarnPlatform.Models;

namespace EarnPlatform.Repositories.Interfaces;

public interface IWithdrawalRequestRepository : IGenericRepository<WithdrawalRequest>
{
    Task<List<WithdrawalRequest>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);

    Task<List<WithdrawalRequest>> GetPendingAsync(CancellationToken cancellationToken = default);


    /// <summary>
    /// Returns every withdrawal request (any status) with the requesting user
    /// included, newest first. Used by the admin withdrawals page which shows
    /// Pending/Approved/Rejected/All tabs plus a "today only" filter.
    /// </summary>
    Task<List<WithdrawalRequest>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);
}