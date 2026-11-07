namespace EarnPlatform.Common.Exceptions;

public sealed class UnauthorizedAppException : AppException
{
    public UnauthorizedAppException(string message = "Authentication required.")
        : base(message, "unauthorized") { }
}