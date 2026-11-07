using EarnPlatform.Api.Dtos;
using EarnPlatform.Api.Helpers;
using EarnPlatform.Repositories.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EarnPlatform.Api.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize(Policy = "RequireAuthenticatedApi")]
public class ProfileController : ControllerBase
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IConfiguration _config;

    public ProfileController(IUserAccountRepository userAccountRepository, IConfiguration config)
    {
        _userAccountRepository = userAccountRepository;
        _config = config;
    }

    [HttpGet]
    public async Task<ActionResult<ProfileDto>> Get(CancellationToken cancellationToken)
    {
        var account = await User.GetUserAccountAsync(_userAccountRepository, cancellationToken);
        if (account == null)
            return Unauthorized(new ApiErrorResponse { Message = "User account not found." });

        // Load package name if needed
        var withPackage = await _userAccountRepository.GetWithPackageAsync(account.Id, cancellationToken)
            ?? account;

        var publicBase = _config["Jwt:Issuer"]?.TrimEnd('/')
            ?? "https://digital-marketingpk.com";

        return Ok(new ProfileDto
        {
            UserAccountId = withPackage.Id,
            Email = withPackage.Email,
            FirstName = withPackage.FirstName,
            LastName = withPackage.LastName,
            ReferralCode = withPackage.ReferralCode,
            ReferralLink = $"{publicBase}/signup?ref={withPackage.ReferralCode}",
            AccountStatus = withPackage.AccountStatus.ToString(),
            AccountLevel = withPackage.AccountLevel,
            SuccessfulReferralCount = withPackage.SuccessfulReferralCount,
            SelectedPackageId = withPackage.SelectedPackageId,
            PackageName = withPackage.SelectedPackage?.Name,
            DailyAdLimit = withPackage.SelectedPackage?.DailyAdLimit,
            CreatedAt = withPackage.CreatedAt,
            ApprovedAt = withPackage.ApprovedAt,
            LastLoginAt = withPackage.LastLoginAt
        });
    }
}