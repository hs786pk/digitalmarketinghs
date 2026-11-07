using System.Text.RegularExpressions;
using EarnPlatform.Factories.Interfaces;
using EarnPlatform.Models;

namespace EarnPlatform.Factories.Implementations;

public class JazzCashProcessor : IWithdrawalProcessor
{
    public bool IsValidAccountNumber(string accountNumber) =>
        Regex.IsMatch(accountNumber, @"^03\d{9}$"); // Pakistani mobile wallet format
}

public class EasypaisaProcessor : IWithdrawalProcessor
{
    public bool IsValidAccountNumber(string accountNumber) =>
        Regex.IsMatch(accountNumber, @"^03\d{9}$");
}

public class WithdrawalProcessorFactory : IWithdrawalProcessorFactory
{
    public IWithdrawalProcessor GetProcessor(PaymentMethod method)
    {
        return method switch
        {
            PaymentMethod.JazzCash => new JazzCashProcessor(),
            PaymentMethod.Easypaisa => new EasypaisaProcessor(),
            _ => throw new NotSupportedException($"Payment method '{method}' is not supported.")
        };
    }
}