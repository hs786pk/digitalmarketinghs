using EarnPlatform.Models;

namespace EarnPlatform.Services.Interfaces;

public interface IWithdrawalService
{
    Task<WithdrawalRequest> SubmitWithdrawalRequestAsync(int userId, PaymentMethod method, string accountNumber, decimal amount, CancellationToken cancellationToken = default);
    Task<List<WithdrawalRequest>> GetUserHistoryAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns every withdrawal request in the system (any status), newest
    /// first, for the admin withdrawals page's Pending/Approved/Rejected/All
    /// tabs and "today only" filter.
    /// </summary>
    Task<List<WithdrawalRequest>> GetAllRequestsAsync(CancellationToken cancellationToken = default);

    Task ApproveAsync(int withdrawalRequestId, string? remarks, CancellationToken cancellationToken = default);
    Task RejectAsync(int withdrawalRequestId, string remarks, CancellationToken cancellationToken = default);
}