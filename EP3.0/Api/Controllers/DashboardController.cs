using EarnPlatform.Api.Dtos;
using EarnPlatform.Api.Helpers;
using EarnPlatform.Repositories.Interfaces;
using EarnPlatform.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EarnPlatform.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = "RequireActiveUserApi")]
public class DashboardController : ControllerBase
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IWalletService _walletService;
    private readonly IConfiguration _config;

    public DashboardController(
        IUserAccountRepository userAccountRepository,
        IWalletService walletService,
        IConfiguration config)
    {
        _userAccountRepository = userAccountRepository;
        _walletService = walletService;
        _config = config;
    }

    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Get(CancellationToken cancellationToken)
    {
        var account = await User.GetUserAccountAsync(_userAccountRepository, cancellationToken);
        if (account == null)
            return Unauthorized(new ApiErrorResponse { Message = "User account not found." });

        var withPackage = await _userAccountRepository.GetWithPackageAsync(account.Id, cancellationToken)
            ?? account;

        var wallet = await _walletService.GetOrCreateWalletAsync(withPackage.Id, cancellationToken);

        var publicBase = _config["Jwt:Issuer"]?.TrimEnd('/')
            ?? "https://digital-marketingpk.com";

        return Ok(new DashboardDto
        {
            FirstName = withPackage.FirstName,
            AccountStatus = withPackage.AccountStatus.ToString(),
            ReferralCode = withPackage.ReferralCode,
            ReferralLink = $"{publicBase}/signup?ref={withPackage.ReferralCode}",
            Balance = wallet.Balance,
            CurrentEarning = wallet.CurrentEarning,
            ReferralEarning = wallet.ReferralEarning,
            PendingEarning = wallet.PendingEarning,
            ApprovedEarning = wallet.ApprovedEarning,
            AccountLevel = withPackage.AccountLevel,
            SelectedPackageId = withPackage.SelectedPackageId,
            PackageName = withPackage.SelectedPackage?.Name
        });
    }
}