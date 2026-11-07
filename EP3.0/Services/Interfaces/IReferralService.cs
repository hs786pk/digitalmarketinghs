using EarnPlatform.Models;

namespace EarnPlatform.Services.Interfaces;

/// <summary>
/// DirectReferrals = everyone who registered with this user's referral code.
/// ActiveTeamMembers = the subset whose payment/package was actually approved
/// (i.e. converted into an active, earning team member). These were previously
/// both populated with the same value; see ReferralService.GetTeamStatsAsync.
/// </summary>
public record TeamStats(int ActiveTeamMembers, int DirectReferrals, int AccountLevel, int DailyAdLimit);

public interface IReferralService
{
    Task<UserAccount?> ValidateReferralCodeAsync(string? referralCode, CancellationToken cancellationToken = default);
    Task<List<UserAccount>> GetReferredUsersAsync(int referrerUserId, CancellationToken cancellationToken = default);
    Task<TeamStats> GetTeamStatsAsync(int userId, CancellationToken cancellationToken = default);
}