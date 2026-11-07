using EarnPlatform.Api.Dtos;
using EarnPlatform.Api.Helpers;
using EarnPlatform.Repositories.Interfaces;
using EarnPlatform.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EarnPlatform.Api.Controllers;

[ApiController]
[Route("api/team")]
[Authorize(Policy = "RequireActiveUserApi")]
public class TeamController : ControllerBase
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IReferralService _referralService;

    public TeamController(
        IUserAccountRepository userAccountRepository,
        IReferralService referralService)
    {
        _userAccountRepository = userAccountRepository;
        _referralService = referralService;
    }

    [HttpGet]
    public async Task<ActionResult<TeamDto>> Get(CancellationToken cancellationToken)
    {
        var account = await User.GetUserAccountAsync(_userAccountRepository, cancellationToken);
        if (account == null)
            return Unauthorized(new ApiErrorResponse { Message = "User account not found." });

        var stats = await _referralService.GetTeamStatsAsync(account.Id, cancellationToken);
        var members = await _referralService.GetReferredUsersAsync(account.Id, cancellationToken);

        return Ok(new TeamDto
        {
            ActiveTeamMembers = stats.ActiveTeamMembers,
            DirectReferrals = stats.DirectReferrals,
            AccountLevel = stats.AccountLevel,
            DailyAdLimit = stats.DailyAdLimit,
            Members = members.Select(m => new TeamMemberDto
            {
                FirstName = m.FirstName,
                LastName = m.LastName,
                JoinedAt = m.CreatedAt,
                PackageName = m.SelectedPackage?.Name,
                AccountStatus = m.AccountStatus.ToString()
            }).ToList()
        });
    }
}