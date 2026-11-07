using EarnPlatform.Common;
using EarnPlatform.Common.Exceptions;
using EarnPlatform.Factories.Interfaces;
using EarnPlatform.Models;
using EarnPlatform.Repositories.Interfaces;
using EarnPlatform.Services.Interfaces;

namespace EarnPlatform.Services.Implementations;

public class WithdrawalService : IWithdrawalService
{
    private readonly IWithdrawalRequestRepository _withdrawalRepository;
    private readonly IWalletService _walletService;
    private readonly IWithdrawalProcessorFactory _processorFactory;
    private readonly IUserAccountRepository _userAccountRepository;

    public WithdrawalService(
        IWithdrawalRequestRepository withdrawalRepository,
        IWalletService walletService,
        IWithdrawalProcessorFactory processorFactory,
        IUserAccountRepository userAccountRepository)
    {
        _withdrawalRepository = withdrawalRepository;
        _walletService = walletService;
        _processorFactory = processorFactory;
        _userAccountRepository = userAccountRepository;
    }

    public async Task<WithdrawalRequest> SubmitWithdrawalRequestAsync(
        int userId,
        PaymentMethod method,
        string accountNumber,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        accountNumber = accountNumber?.Trim() ?? string.Empty;
        amount = decimal.Round(amount, 2, MidpointRounding.ToEven);

        if (amount <= 0)
            throw new ValidationException("Withdrawal amount must be positive.");

        if (amount < BusinessRules.MinimumWithdrawalAmount)
            throw new ValidationException(
                $"The minimum withdrawal amount is PKR {BusinessRules.MinimumWithdrawalAmount:N0}.");

        var user = await _userAccountRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        if (user.AccountStatus != AccountStatus.Active)
            throw new ForbiddenException("Only active accounts can request withdrawals.");

        if (user.SelectedPackageId == null)
            throw new ForbiddenException("You must have an active package before requesting a withdrawal.");

        if (user.AccountLevel < BusinessRules.MinimumWithdrawalLevel)
            throw new ForbiddenException(
                $"You must reach Level {BusinessRules.MinimumWithdrawalLevel} to withdraw funds. Your current level is {user.AccountLevel}.");

        var processor = _processorFactory.GetProcessor(method);
        if (!processor.IsValidAccountNumber(accountNumber))
            throw new ValidationException($"'{accountNumber}' is not a valid {method} account number.");

        return await _walletService.SubmitWithdrawalAsync(new WithdrawalRequest
        {
            UserId = userId,
            PaymentMethod = method,
            AccountNumber = accountNumber,
            Amount = amount,
            Status = WithdrawalStatus.Pending,
            RequestedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    public Task<List<WithdrawalRequest>> GetUserHistoryAsync(
        int userId, CancellationToken cancellationToken = default) =>
        _withdrawalRepository.GetByUserIdAsync(userId, cancellationToken);

    public Task<List<WithdrawalRequest>> GetAllRequestsAsync(
        CancellationToken cancellationToken = default) =>
        _withdrawalRepository.GetAllWithDetailsAsync(cancellationToken);

    public Task ApproveAsync(
        int withdrawalRequestId, string? remarks, CancellationToken cancellationToken = default) =>
        _walletService.ApproveWithdrawalAsync(withdrawalRequestId, remarks?.Trim(), cancellationToken);

    public Task RejectAsync(
        int withdrawalRequestId, string remarks, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(remarks))
            throw new ValidationException("A rejection reason is required.");
        return _walletService.RejectWithdrawalAsync(withdrawalRequestId, remarks.Trim(), cancellationToken);
    }
}