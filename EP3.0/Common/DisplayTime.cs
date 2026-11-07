namespace EarnPlatform.Common;

/// <summary>
/// Formats persisted UTC timestamps for display in the application's business
/// timezone (Asia/Karachi). Prefer this over <c>DateTime.ToLocalTime()</c>, which
/// depends on the host OS timezone and is incorrect when the server is not
/// running in Pakistan.
/// </summary>
public static class DisplayTime
{
    private static readonly TimeZoneInfo PakistanTimeZone = ResolvePakistanTimeZone();

    public static DateTime ToBusinessLocal(DateTime utc)
    {
        var value = utc.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(utc, DateTimeKind.Utc)
            : utc.ToUniversalTime();
        return TimeZoneInfo.ConvertTimeFromUtc(value, PakistanTimeZone);
    }

    public static string Format(DateTime utc, string format = "dd MMM yyyy") =>
        ToBusinessLocal(utc).ToString(format);

    public static string FormatDateTime(DateTime utc) =>
        Format(utc, "dd MMM yyyy, hh:mm tt");

    public static DateTime BusinessToday =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, PakistanTimeZone).Date;

    private static TimeZoneInfo ResolvePakistanTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Karachi"); }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Pakistan Standard Time");
        }
    }
}
