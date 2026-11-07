using EarnPlatform.Models;
using EarnPlatform.Repositories.Interfaces;
using EarnPlatform.Services.Interfaces;

namespace EarnPlatform.Services.Implementations;

public class ReferralService : IReferralService
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IReferralRepository _referralRepository;

    public ReferralService(IUserAccountRepository userAccountRepository, IReferralRepository referralRepository)
    { _userAccountRepository = userAccountRepository; _referralRepository = referralRepository; }

    public async Task<UserAccount?> ValidateReferralCodeAsync(string? referralCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(referralCode)) return null;
        return await _userAccountRepository.GetByReferralCodeAsync(referralCode.Trim().ToUpperInvariant(), cancellationToken);
    }


    public Task<List<UserAccount>> GetReferredUsersAsync(int referrerUserId, CancellationToken cancellationToken = default) => _userAccountRepository.GetReferredUsersAsync(referrerUserId, cancellationToken);

    public async Task<TeamStats> GetTeamStatsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _userAccountRepository.GetWithPackageAsync(userId, cancellationToken) ?? throw new InvalidOperationException("User not found.");
        var directReferrals = await _referralRepository.CountDirectReferralsAsync(userId, cancellationToken);
        // Active team members = referrals that reached Successful status.
        // Prefer the live table count over the denormalized counter so the UI
        // always reflects actual referral rows (the counter remains the source
        // of truth for level-up progression and is updated atomically with the
        // status transition).
        var successful = await _referralRepository.CountSuccessfulReferralsAsync(userId, cancellationToken);
        return new TeamStats(successful, directReferrals, user.AccountLevel, user.SelectedPackage?.DailyAdLimit ?? 0);
    }

}
