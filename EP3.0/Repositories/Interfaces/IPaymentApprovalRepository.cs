using EarnPlatform.Models;

namespace EarnPlatform.Repositories.Interfaces;

public interface IPaymentApprovalRepository : IGenericRepository<PaymentApproval>
{
    Task<List<PaymentApproval>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task<PaymentApproval?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default);
}