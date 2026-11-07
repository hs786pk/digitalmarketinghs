using EarnPlatform.Factories.Interfaces;
using EarnPlatform.Models;

namespace EarnPlatform.Factories.Implementations;

public class WalletTransactionFactory : IWalletTransactionFactory
{
    public WalletTransaction CreateWorkRewardTransaction(int userId, decimal amount, int workTaskId)
    {
        return NewTransaction(userId, amount, WalletTransactionType.WorkReward,
            $"Reward credited for completing task #{workTaskId}", WalletTransactionStatus.Approved, approve: true);
    }

    public WalletTransaction CreateReferralBonusTransaction(int userId, decimal amount, int referredUserId)
    {
        return NewTransaction(userId, amount, WalletTransactionType.ReferralBonus,
            $"Referral bonus for user #{referredUserId} joining", WalletTransactionStatus.Approved, approve: true);
    }

    public WalletTransaction CreateWithdrawalDebitTransaction(int userId, decimal amount, int withdrawalRequestId)
    {
        // Negative amount: this transaction represents money leaving the wallet
        return NewTransaction(userId, -amount, WalletTransactionType.WithdrawalDebit,
            $"Withdrawal request #{withdrawalRequestId} submitted", WalletTransactionStatus.Pending, approve: false);
    }

    public WalletTransaction CreateWithdrawalReversalTransaction(int userId, decimal amount, int withdrawalRequestId, string reason)
    {
        // Positive amount: reverses a prior debit when a withdrawal is rejected
        return NewTransaction(userId, amount, WalletTransactionType.WithdrawalReversal,
            $"Withdrawal request #{withdrawalRequestId} rejected: {reason}", WalletTransactionStatus.Approved, approve: true);
    }

    public WalletTransaction CreatePackagePurchaseCreditTransaction(int userId, decimal amount, int packageId)
    {
        return NewTransaction(userId, amount, WalletTransactionType.PackagePurchaseCredit,
            $"Package #{packageId} activated", WalletTransactionStatus.Approved, approve: true);
    }


    private static WalletTransaction NewTransaction(
        int userId, decimal amount, WalletTransactionType type, string description,
        WalletTransactionStatus status, bool approve)
    {
        var now = DateTime.UtcNow;
        return new WalletTransaction
        {
            UserId = userId,
            Amount = amount,
            TransactionType = type,
            Description = description,
            Status = status,
            CreatedAt = now,
            ApprovedAt = approve ? now : null
        };
    }
}