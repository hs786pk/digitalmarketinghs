using EarnPlatform.Models;

namespace EarnPlatform.Services.Interfaces;

/// <summary>
/// The single gateway for every wallet balance change in the system. No other
/// service or Razor component is permitted to modify Wallet.Balance directly —
/// this keeps every change transactional and paired with a WalletTransaction
/// audit row (spec section 32).
/// </summary>
public interface IWalletService
{
    Task<Wallet> GetOrCreateWalletAsync(int userId, CancellationToken cancellationToken = default);

    Task CompleteWorkTaskAsync(int userId, int completionId, CancellationToken cancellationToken = default);
    Task<WithdrawalRequest> SubmitWithdrawalAsync(WithdrawalRequest request, CancellationToken cancellationToken = default);
    Task ApproveWithdrawalAsync(int withdrawalRequestId, string? remarks, CancellationToken cancellationToken = default);
    Task RejectWithdrawalAsync(int withdrawalRequestId, string remarks, CancellationToken cancellationToken = default);
    Task ApprovePaymentAsync(int paymentApprovalId, CancellationToken cancellationToken = default);
    Task RejectPaymentAsync(int paymentApprovalId, string remarks, CancellationToken cancellationToken = default);
}