using System.Data;
using EarnPlatform.Common.Exceptions;
using EarnPlatform.Data;
using EarnPlatform.Factories.Interfaces;
using EarnPlatform.Models;
using EarnPlatform.Repositories.Interfaces;
using EarnPlatform.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EarnPlatform.Services.Implementations;

public class WalletService : IWalletService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly IWalletRepository _walletRepository;
    private readonly IWalletTransactionFactory _transactionFactory;
    private readonly ILogger<WalletService> _logger;
    private readonly IBusinessClock _clock;

    public WalletService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        IWalletRepository walletRepository,
        IWalletTransactionFactory transactionFactory,
        ILogger<WalletService> logger,
        IBusinessClock clock)
    {
        _dbFactory = dbFactory;
        _walletRepository = walletRepository;
        _transactionFactory = transactionFactory;
        _logger = logger;
        _clock = clock;
    }

    public async Task<Wallet> GetOrCreateWalletAsync(int userId, CancellationToken cancellationToken = default)
    {
        var wallet = await _walletRepository.GetByUserIdAsync(userId, cancellationToken);
        if (wallet != null) return wallet;

        var newWallet = new Wallet { UserId = userId, UpdatedAt = DateTime.UtcNow };
        try
        {
            await _walletRepository.AddAsync(newWallet, cancellationToken);
            return newWallet;
        }
        catch (DbUpdateException)
        {
            var existing = await _walletRepository.GetByUserIdAsync(userId, cancellationToken);
            if (existing != null) return existing;
            throw;
        }
    }

    public async Task CompleteWorkTaskAsync(int userId, int completionId, CancellationToken cancellationToken = default)
    {
        string? rejection = null;
        await ExecuteInTransactionAsync(async context =>
        {
            var user = await context.UserAccounts
                .FromSqlInterpolated($"SELECT * FROM [UserAccounts] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {userId}")
                .Include(u => u.SelectedPackage)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("User not found.");

            var completion = await context.WorkTaskCompletions
                .FromSqlInterpolated($"SELECT * FROM [WorkTaskCompletions] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {completionId}")
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("Task session not found.");

            if (completion.UserId != userId)
                throw new ForbiddenException("This task session does not belong to the current user.");

            if (completion.Status == WorkTaskCompletionStatus.Credited)
                return;

            if (completion.Status != WorkTaskCompletionStatus.Started)
                throw new ConflictException("This task session is not in a valid state to complete.");

            var now = _clock.UtcNow;
            if (!Common.BusinessRules.IsActiveSession(completion.StartedAt, now))
            {
                completion.Status = WorkTaskCompletionStatus.Expired;
                completion.CompletedAt = now;
                rejection = "This task session has expired. Please start the task again.";
                await context.SaveChangesAsync(cancellationToken);
                return;
            }

            if (user.AccountStatus != AccountStatus.Active || user.SelectedPackage == null)
            {
                completion.Status = WorkTaskCompletionStatus.Expired;
                completion.CompletedAt = now;
                rejection = "Your account is not currently eligible to earn from tasks.";
                await context.SaveChangesAsync(cancellationToken);
                return;
            }

            var businessToday = _clock.TodayUtcStart;
            var tomorrow = _clock.TomorrowUtcStart;
            var creditedToday = await context.WorkTaskCompletions.CountAsync(c =>
                c.UserId == userId &&
                c.Status == WorkTaskCompletionStatus.Credited &&
                c.CompletedAt >= businessToday && c.CompletedAt < tomorrow, cancellationToken);

            if (creditedToday >= user.SelectedPackage.DailyAdLimit)
            {
                completion.Status = WorkTaskCompletionStatus.Expired;
                completion.CompletedAt = now;
                rejection = "Your daily task limit has already been reached.";
                await context.SaveChangesAsync(cancellationToken);
                return;
            }

            var wallet = await GetTrackedWalletAsync(context, userId, cancellationToken);
            wallet.CurrentEarning += completion.RewardAmount;
            wallet.ApprovedEarning += completion.RewardAmount;
            wallet.Balance += completion.RewardAmount;
            wallet.UpdatedAt = now;

            completion.Status = WorkTaskCompletionStatus.Credited;
            completion.CompletedAt = now;

            var transaction = _transactionFactory.CreateWorkRewardTransaction(
                userId, completion.RewardAmount, completion.WorkTaskId);
            await context.WalletTransactions.AddAsync(transaction, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        if (rejection != null)
            throw new ValidationException(rejection);
    }

    public async Task<WithdrawalRequest> SubmitWithdrawalAsync(
        WithdrawalRequest request, CancellationToken cancellationToken = default)
    {
        await ExecuteInTransactionAsync(async context =>
        {
            var wallet = await GetTrackedWalletAsync(context, request.UserId, cancellationToken);
            if (wallet.Balance < request.Amount)
                throw new InsufficientBalanceException();

            await context.WithdrawalRequests.AddAsync(request, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            wallet.Balance -= request.Amount;
            wallet.PendingEarning += request.Amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            var transaction = _transactionFactory.CreateWithdrawalDebitTransaction(
                request.UserId, request.Amount, request.Id);
            await context.WalletTransactions.AddAsync(transaction, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            request.RelatedWalletTransactionId = transaction.Id;
            await context.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        return request;
    }

    public async Task ApproveWithdrawalAsync(
        int withdrawalRequestId, string? remarks, CancellationToken cancellationToken = default)
    {
        await ExecuteInTransactionAsync(async context =>
        {
            var request = await context.WithdrawalRequests
                .FromSqlInterpolated($"SELECT * FROM [WithdrawalRequests] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {withdrawalRequestId}")
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("Withdrawal request not found.");

            if (request.Status != WithdrawalStatus.Pending)
                throw new ConflictException("This withdrawal request has already been processed.");

            var wallet = await GetTrackedWalletAsync(context, request.UserId, cancellationToken);
            if (wallet.PendingEarning < request.Amount)
                throw new ConflictException("The reserved wallet amount is inconsistent with this withdrawal.");

            wallet.PendingEarning -= request.Amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            request.Status = WithdrawalStatus.Approved;
            request.ProcessedAt = DateTime.UtcNow;
            request.AdminRemarks = remarks;

            if (request.RelatedWalletTransactionId.HasValue)
            {
                var debit = await context.WalletTransactions
                    .FromSqlInterpolated($"SELECT * FROM [WalletTransactions] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {request.RelatedWalletTransactionId.Value}")
                    .FirstOrDefaultAsync(cancellationToken);
                if (debit != null)
                {
                    debit.Status = WalletTransactionStatus.Approved;
                    debit.ApprovedAt = DateTime.UtcNow;
                }
            }

            await context.SaveChangesAsync(cancellationToken);
        }, cancellationToken);
    }

    public async Task RejectWithdrawalAsync(
        int withdrawalRequestId, string remarks, CancellationToken cancellationToken = default)
    {
        await ExecuteInTransactionAsync(async context =>
        {
            var request = await context.WithdrawalRequests
                .FromSqlInterpolated($"SELECT * FROM [WithdrawalRequests] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {withdrawalRequestId}")
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("Withdrawal request not found.");

            if (request.Status != WithdrawalStatus.Pending)
                throw new ConflictException("This withdrawal request has already been processed.");

            var wallet = await GetTrackedWalletAsync(context, request.UserId, cancellationToken);
            if (wallet.PendingEarning < request.Amount)
                throw new ConflictException("The reserved wallet amount is inconsistent with this withdrawal.");

            wallet.Balance += request.Amount;
            wallet.PendingEarning -= request.Amount;
            wallet.RejectedEarning += request.Amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            request.Status = WithdrawalStatus.Rejected;
            request.ProcessedAt = DateTime.UtcNow;
            request.AdminRemarks = remarks;

            if (request.RelatedWalletTransactionId.HasValue)
            {
                var debit = await context.WalletTransactions
                    .FromSqlInterpolated($"SELECT * FROM [WalletTransactions] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {request.RelatedWalletTransactionId.Value}")
                    .FirstOrDefaultAsync(cancellationToken);
                if (debit != null)
                    debit.Status = WalletTransactionStatus.Rejected;
            }

            var reversal = _transactionFactory.CreateWithdrawalReversalTransaction(
                request.UserId, request.Amount, request.Id, remarks);
            await context.WalletTransactions.AddAsync(reversal, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }, cancellationToken);
    }

    public async Task ApprovePaymentAsync(int paymentApprovalId, CancellationToken cancellationToken = default)
    {
        await ExecuteInTransactionAsync(async context =>
        {
            var approval = await context.PaymentApprovals
                .FromSqlInterpolated($"SELECT * FROM [PaymentApprovals] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {paymentApprovalId}")
                .Include(p => p.Package)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("Payment approval not found.");

            if (approval.Status != PaymentApprovalStatus.Pending)
                throw new ConflictException("This payment request has already been reviewed.");

            if (!approval.Package.IsActive)
                throw new ValidationException("The selected package is no longer active.");

            if (approval.Amount != approval.Package.Price)
                throw new ValidationException("The payment amount does not match the selected package price.");

            var user = await context.UserAccounts
                .FromSqlInterpolated($"SELECT * FROM [UserAccounts] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {approval.UserId}")
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("User not found.");

            approval.Status = PaymentApprovalStatus.Approved;
            approval.ReviewedAt = DateTime.UtcNow;
            approval.PaymentScreenshotPath = null;

            user.SelectedPackageId = approval.PackageId;
            user.AccountLevel = approval.Package.Level;
            user.AccountStatus = AccountStatus.Active;
            user.ApprovedAt = DateTime.UtcNow;

            var purchaseTransaction = _transactionFactory.CreatePackagePurchaseCreditTransaction(
                user.Id, approval.Amount, approval.PackageId);
            await context.WalletTransactions.AddAsync(purchaseTransaction, cancellationToken);

            var referral = await context.Referrals
                .FromSqlInterpolated($"SELECT * FROM [Referrals] WITH (UPDLOCK, ROWLOCK) WHERE [ReferredUserId] = {user.Id} AND [Status] = {ReferralStatus.Active.ToString()}")
                .FirstOrDefaultAsync(cancellationToken);

            if (referral != null)
            {
                referral.Status = ReferralStatus.Successful;

                var referrerWallet = await GetTrackedWalletAsync(context, referral.ReferrerUserId, cancellationToken);
                referrerWallet.ReferralEarning += Common.BusinessRules.ReferralBonusAmount;
                referrerWallet.ApprovedEarning += Common.BusinessRules.ReferralBonusAmount;
                referrerWallet.Balance += Common.BusinessRules.ReferralBonusAmount;
                referrerWallet.UpdatedAt = DateTime.UtcNow;

                var referralTransaction = _transactionFactory.CreateReferralBonusTransaction(
                    referral.ReferrerUserId,
                    Common.BusinessRules.ReferralBonusAmount,
                    user.Id);
                await context.WalletTransactions.AddAsync(referralTransaction, cancellationToken);

                var referrer = await context.UserAccounts
                    .FromSqlInterpolated($"SELECT * FROM [UserAccounts] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {referral.ReferrerUserId}")
                    .FirstOrDefaultAsync(cancellationToken);
                if (referrer != null)
                {
                    referrer.SuccessfulReferralCount += 1;
                    if (referrer.SuccessfulReferralCount % Common.BusinessRules.ReferralsPerLevelUp == 0)
                        referrer.AccountLevel += 1;
                }
            }

            await context.SaveChangesAsync(cancellationToken);
        }, cancellationToken);
    }

    public async Task RejectPaymentAsync(
        int paymentApprovalId, string remarks, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(remarks))
            throw new ValidationException("A rejection reason is required.");

        await ExecuteInTransactionAsync(async context =>
        {
            var approval = await context.PaymentApprovals
                .FromSqlInterpolated($"SELECT * FROM [PaymentApprovals] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {paymentApprovalId}")
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("Payment approval not found.");

            if (approval.Status != PaymentApprovalStatus.Pending)
                throw new ConflictException("This payment request has already been reviewed.");

            var user = await context.UserAccounts
                .FromSqlInterpolated($"SELECT * FROM [UserAccounts] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {approval.UserId}")
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("User not found.");

            approval.Status = PaymentApprovalStatus.Rejected;
            approval.ReviewedAt = _clock.UtcNow;
            approval.AdminRemarks = remarks.Trim();
            approval.PaymentScreenshotPath = null;
            user.SelectedPackageId = null;
            await context.SaveChangesAsync(cancellationToken);
        }, cancellationToken);
    }

    private static async Task<Wallet> GetTrackedWalletAsync(
        ApplicationDbContext context, int userId, CancellationToken cancellationToken)
    {
        var wallet = await context.Wallets
            .FromSqlInterpolated($"SELECT * FROM [Wallets] WITH (UPDLOCK, ROWLOCK) WHERE [UserId] = {userId}")
            .FirstOrDefaultAsync(cancellationToken);

        if (wallet == null)
            throw new NotFoundException($"Wallet not found for user {userId}.");

        return wallet;
    }

    private async Task ExecuteInTransactionAsync(
        Func<ApplicationDbContext, Task> action, CancellationToken cancellationToken)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var strategy = context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted, cancellationToken);
            try
            {
                await action(context);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (AppException)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw; // expected business failure — API filter / UI maps it
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Wallet transaction rolled back.");
                throw;
            }
        });
    }
}