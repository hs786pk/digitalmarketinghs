using System.Data;
using EarnPlatform.Common.Exceptions;
using EarnPlatform.Data;
using EarnPlatform.Models;
using EarnPlatform.Repositories.Interfaces;
using EarnPlatform.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EarnPlatform.Services.Implementations;

public class PaymentApprovalService : IPaymentApprovalService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly IPaymentApprovalRepository _paymentApprovalRepository;
    private readonly IFileUploadService _fileUploadService;
    private readonly IWalletService _walletService;
    private readonly ISettingsService _settingsService;

    public PaymentApprovalService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        IPaymentApprovalRepository paymentApprovalRepository,
        IFileUploadService fileUploadService,
        IWalletService walletService,
        ISettingsService settingsService)
    {
        _dbFactory = dbFactory;
        _paymentApprovalRepository = paymentApprovalRepository;
        _fileUploadService = fileUploadService;
        _walletService = walletService;
        _settingsService = settingsService;
    }

    public async Task<PaymentApproval> SubmitPaymentAsync(
        int userId, int packageId, string accountName, string transactionId, string screenshotPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(screenshotPath))
            throw new ValidationException("Payment screenshot is required.");
        accountName = accountName?.Trim() ?? string.Empty;
        transactionId = transactionId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(accountName))
            throw new ValidationException("Account name is required.");
        if (string.IsNullOrWhiteSpace(transactionId))
            throw new ValidationException("Transaction ID is required.");

        var paymentSettings = await _settingsService.GetPaymentSettingsAsync(cancellationToken);
        var accountNumber = paymentSettings.PaymentAccountNumber?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(accountNumber))
            throw new ValidationException("Payment receiving account is not configured. Please contact support.");

        await using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var strategy = context.Database.CreateExecutionStrategy();
        PaymentApproval? result = null;

        try
        {
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable, cancellationToken);

                var user = await context.UserAccounts
                    .FromSqlInterpolated($"SELECT * FROM [UserAccounts] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {userId}")
                    .FirstOrDefaultAsync(cancellationToken)
                    ?? throw new NotFoundException("User not found.");

                if (user.AccountStatus == AccountStatus.Active)
                    throw new ConflictException("Your account is already active.");

                var package = await context.Packages.FirstOrDefaultAsync(p => p.Id == packageId, cancellationToken)
                    ?? throw new NotFoundException("Selected package not found.");
                if (!package.IsActive)
                    throw new ValidationException("Selected package is no longer active.");

                var amount = decimal.Round(package.Price, 2, MidpointRounding.ToEven);

                var hasPending = await context.PaymentApprovals.AnyAsync(
                    p => p.UserId == userId && p.Status == PaymentApprovalStatus.Pending, cancellationToken);
                if (hasPending)
                    throw new ConflictException("You already have a payment awaiting admin review.");

                var duplicateTransaction = await context.PaymentApprovals.AnyAsync(
                    p => p.TransactionId == transactionId && p.Status != PaymentApprovalStatus.Rejected, cancellationToken);
                if (duplicateTransaction)
                    throw new ConflictException("This transaction ID has already been submitted.");

                result = new PaymentApproval
                {
                    UserId = userId,
                    PackageId = packageId,
                    Amount = amount,
                    AccountNumber = accountNumber,
                    AccountName = accountName,
                    TransactionId = transactionId,
                    PaymentScreenshotPath = screenshotPath,
                    Status = PaymentApprovalStatus.Pending,
                    SubmittedAt = DateTime.UtcNow
                };

                await context.PaymentApprovals.AddAsync(result, cancellationToken);
                user.SelectedPackageId = packageId;
                await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            });
            return result!;
        }
        catch
        {
            _fileUploadService.DeleteFile(screenshotPath);
            throw;
        }
    }

    public Task<List<PaymentApproval>> GetPendingApprovalsAsync(CancellationToken cancellationToken = default) =>
        _paymentApprovalRepository.GetPendingAsync(cancellationToken);

    public async Task ApproveAsync(int paymentApprovalId, CancellationToken cancellationToken = default)
    {
        var approval = await _paymentApprovalRepository.GetWithDetailsAsync(paymentApprovalId, cancellationToken)
            ?? throw new NotFoundException("Payment approval not found.");
        if (approval.Status != PaymentApprovalStatus.Pending)
            throw new ConflictException("This payment request has already been reviewed.");

        var screenshotPath = approval.PaymentScreenshotPath;
        await _walletService.ApprovePaymentAsync(paymentApprovalId, cancellationToken);
        _fileUploadService.DeleteFile(screenshotPath);
    }

    public async Task RejectAsync(int paymentApprovalId, string remarks, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(remarks))
            throw new ValidationException("A rejection reason is required.");
        var approval = await _paymentApprovalRepository.GetByIdAsync(paymentApprovalId, cancellationToken)
            ?? throw new NotFoundException("Payment approval not found.");
        var screenshotPath = approval.PaymentScreenshotPath;
        await _walletService.RejectPaymentAsync(paymentApprovalId, remarks.Trim(), cancellationToken);
        _fileUploadService.DeleteFile(screenshotPath);
    }
}