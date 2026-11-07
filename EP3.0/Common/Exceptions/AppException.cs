namespace EarnPlatform.Common.Exceptions;

/// <summary>Base for expected business failures (safe to show message to clients).</summary>
public abstract class AppException : Exception
{
    public string? ErrorCode { get; }

    protected AppException(string message, string? errorCode = null, Exception? inner = null)
        : base(message, inner)
    {
        ErrorCode = errorCode;
    }
}