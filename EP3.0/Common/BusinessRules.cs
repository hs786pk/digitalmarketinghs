namespace EarnPlatform.Common;

/// <summary>
/// Centralized business-rule constants. Keeping these in one place avoids
/// magic numbers scattered across services and makes future tuning (e.g.
/// changing the referral bonus amount) a one-line change.
/// </summary>
public static class BusinessRules
{
    /// <summary>Flat wallet credit paid to a referrer for each referred user who gets approved.</summary>
    public const decimal ReferralBonusAmount = 100m;

    /// <summary>Number of successful referrals required to auto-increment the referrer's account level by 1.</summary>
    public const int ReferralsPerLevelUp = 3;

    /// <summary>Smallest amount a user may request as a single withdrawal.</summary>
    public const decimal MinimumWithdrawalAmount = 300m;

    /// <summary>Minimum account level required before a withdrawal request can be submitted.</summary>
    public const int MinimumWithdrawalLevel = 5;

    /// <summary>
    /// How long a "Started" work-task session remains active before it is considered
    /// stale/expired. Single source of truth for both the in-memory helper
    /// (<see cref="IsActiveSession"/>) and EF Core query predicates — do not
    /// hardcode this value (e.g. TimeSpan.FromMinutes(30)) anywhere else.
    /// </summary>
    public static readonly TimeSpan TaskSessionLifetime = TimeSpan.FromMinutes(30);

    /// <summary>
    /// A session is active only when it has already started (not a future timestamp)
    /// and is still within the lifetime window. Callers that also have a status must
    /// additionally require Status == Started. EF queries express the same bounds
    /// with StartedAt &lt;= now &amp;&amp; StartedAt &gt;= now - lifetime.
    /// </summary>
    public static bool IsActiveSession(DateTime startedAt, DateTime now) =>
        // Inclusive lower bound so that StartedAt == now - lifetime is still active
        // (matches IsSessionExpired which treats exact equality as not-yet-expired).
        startedAt <= now && startedAt >= now - TaskSessionLifetime;
}