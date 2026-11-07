namespace EarnPlatform.Common.Exceptions;

public sealed class ConcurrencyException : AppException
{
    public ConcurrencyException(string message = "The record was modified by another request. Please try again.")
        : base(message, "concurrency_conflict") { }
}