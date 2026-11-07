using EarnPlatform.Models;

namespace EarnPlatform.Factories.Interfaces;

/// <summary>
/// Factory Pattern: every wallet-affecting event in the platform (work reward,
/// referral bonus, withdrawal debit, withdrawal reversal, admin adjustment)
/// needs a WalletTransaction shaped slightly differently (sign of Amount,
/// default Status, Description text). Centralizing that here means
/// WalletService only ever calls one of these factory methods and never
/// hand-builds a WalletTransaction with a typo'd sign or wrong status.
/// </summary>
public interface IWalletTransactionFactory
{
    WalletTransaction CreateWorkRewardTransaction(int userId, decimal amount, int workTaskId);
    WalletTransaction CreateReferralBonusTransaction(int userId, decimal amount, int referredUserId);
    WalletTransaction CreateWithdrawalDebitTransaction(int userId, decimal amount, int withdrawalRequestId);
    WalletTransaction CreateWithdrawalReversalTransaction(int userId, decimal amount, int withdrawalRequestId, string reason);
    WalletTransaction CreatePackagePurchaseCreditTransaction(int userId, decimal amount, int packageId);
}