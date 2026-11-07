using EarnPlatform.Models;

namespace EarnPlatform.Factories.Interfaces;

public interface IWithdrawalProcessor
{
    /// <summary>Validates the account number format for this specific payment method.</summary>
    bool IsValidAccountNumber(string accountNumber);
}

/// <summary>
/// Factory Pattern: selects the correct IWithdrawalProcessor strategy based on
/// PaymentMethod. Today both JazzCash and Easypaisa only validate account
/// number format, but this is the seam where real gateway API calls would be
/// added later — WalletService only ever depends on the interface.
/// </summary>
public interface IWithdrawalProcessorFactory
{
    IWithdrawalProcessor GetProcessor(PaymentMethod method);
}