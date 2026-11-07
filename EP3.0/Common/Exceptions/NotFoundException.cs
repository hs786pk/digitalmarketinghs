namespace EarnPlatform.Common.Exceptions;

public sealed class NotFoundException : AppException
{
    public NotFoundException(string message)
        : base(message, "not_found") { }
}