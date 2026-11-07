namespace EarnPlatform.Infrastructure;

/// <summary>
/// Structured log helpers for money/auth operations. Never pass passwords, JWTs, or raw tokens.
/// </summary>
public static class AppLog
{
    public static void Operation(
        ILogger logger,
        LogLevel level,
        string operation,
        string result,
        int? userId = null,
        decimal? amount = null,
        int? entityId = null,
        string? reason = null,
        long? durationMs = null,
        string? extra = null)
    {
        logger.Log(
            level,
            "Op={Operation} Result={Result} UserId={UserId} Amount={Amount} EntityId={EntityId} Reason={Reason} DurationMs={DurationMs} Detail={Detail}",
            operation,
            result,
            userId,
            amount,
            entityId,
            reason,
            durationMs,
            extra);
    }

    public static void WithdrawalFailed(ILogger logger, int userId, decimal amount, int? withdrawalId, string reason) =>
        Operation(logger, LogLevel.Warning, "Withdrawal", "Failed", userId, amount, withdrawalId, reason);

    public static void WithdrawalSucceeded(ILogger logger, int userId, decimal amount, int withdrawalId) =>
        Operation(logger, LogLevel.Information, "Withdrawal", "Succeeded", userId, amount, withdrawalId);

    public static void PaymentSubmitted(ILogger logger, int userId, decimal amount, int paymentId) =>
        Operation(logger, LogLevel.Information, "PaymentSubmit", "Succeeded", userId, amount, paymentId);

    public static void PaymentApproved(ILogger logger, int userId, decimal amount, int paymentId) =>
        Operation(logger, LogLevel.Information, "PaymentApprove", "Succeeded", userId, amount, paymentId);

    public static void PaymentRejected(ILogger logger, int userId, int paymentId, string reason) =>
        Operation(logger, LogLevel.Information, "PaymentReject", "Succeeded", userId, entityId: paymentId, reason: reason);

    public static void TaskCredited(ILogger logger, int userId, decimal amount, int completionId) =>
        Operation(logger, LogLevel.Information, "TaskComplete", "Credited", userId, amount, completionId);
}