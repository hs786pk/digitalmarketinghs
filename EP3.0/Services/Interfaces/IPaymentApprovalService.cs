using EarnPlatform.Models;

namespace EarnPlatform.Services.Interfaces;

public interface IPaymentApprovalService
{
    /// <param name="accountName">Sender/account name supplied by the user.</param>
    /// <remarks>
    /// Receiving (platform) account number is loaded server-side from AppSettings —
    /// never accepted from the caller.
    /// </remarks>
    Task<PaymentApproval> SubmitPaymentAsync(int userId, int packageId, string accountName, string transactionId, string screenshotPath, CancellationToken cancellationToken = default);
    Task<List<PaymentApproval>> GetPendingApprovalsAsync(CancellationToken cancellationToken = default);
    Task ApproveAsync(int paymentApprovalId, CancellationToken cancellationToken = default);
    Task RejectAsync(int paymentApprovalId, string remarks, CancellationToken cancellationToken = default);
}
