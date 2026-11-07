namespace EarnPlatform.Common.Exceptions;

public sealed class InsufficientBalanceException : AppException
{
    public InsufficientBalanceException(string message = "Withdrawal amount exceeds your available balance.")
        : base(message, "insufficient_balance") { }
}