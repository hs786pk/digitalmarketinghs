namespace EarnPlatform.Models;

public enum AccountStatus
{
    PendingApproval,  // registered / awaiting payment review
    Active,           // approved earner
    Rejected,
    Suspended
}
public enum PaymentApprovalStatus
{
    Pending,
    Approved,
    Rejected
}

public enum WithdrawalStatus
{
    Pending,
    Approved,
    Rejected
}

public enum PaymentMethod
{
    JazzCash,
    Easypaisa
}

public enum WalletTransactionType
{
    WorkReward,
    ReferralBonus,
    StaffBonus,
    WithdrawalDebit,
    WithdrawalReversal,
    PackagePurchaseCredit,
    AdminAdjustment
}

public enum WalletTransactionStatus
{
    Pending,
    Approved,
    Rejected
}

public enum ReferralStatus
{
    Active,
    Successful,
    Invalid
}

public enum WorkTaskCompletionStatus
{
    Started,
    Credited,
    Expired
}