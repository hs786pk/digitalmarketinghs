namespace EarnPlatform.Common.Exceptions;

public sealed class ValidationException : AppException
{
    public IReadOnlyList<string>? Errors { get; }

    public ValidationException(string message, IEnumerable<string>? errors = null)
        : base(message, "validation_error")
    {
        Errors = errors?.ToList();
    }
}