namespace EarnPlatform.Services.Interfaces;

public interface IBusinessClock
{
    DateTime UtcNow { get; }
    DateTime LocalNow { get; }
    DateTime Today { get; }
    DateTime TodayUtcStart { get; }
    DateTime TomorrowUtcStart { get; }
}
