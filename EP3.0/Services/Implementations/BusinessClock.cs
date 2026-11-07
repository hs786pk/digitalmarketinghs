using EarnPlatform.Services.Interfaces;

namespace EarnPlatform.Services.Implementations;

public sealed class BusinessClock : IBusinessClock
{
    private static readonly TimeZoneInfo PakistanTimeZone = ResolvePakistanTimeZone();

    public DateTime UtcNow => DateTime.UtcNow;
    public DateTime LocalNow => TimeZoneInfo.ConvertTimeFromUtc(UtcNow, PakistanTimeZone);
    public DateTime Today => LocalNow.Date;
    public DateTime TodayUtcStart => TimeZoneInfo.ConvertTimeToUtc(Today, PakistanTimeZone);
    public DateTime TomorrowUtcStart => TimeZoneInfo.ConvertTimeToUtc(Today.AddDays(1), PakistanTimeZone);

    private static TimeZoneInfo ResolvePakistanTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Karachi"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Pakistan Standard Time"); }
    }
}
