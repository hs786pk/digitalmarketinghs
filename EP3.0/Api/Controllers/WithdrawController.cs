using EarnPlatform.Api.Dtos;
using EarnPlatform.Api.Helpers;
using EarnPlatform.Common;
using EarnPlatform.Repositories.Interfaces;
using EarnPlatform.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EarnPlatform.Api.Controllers;

[ApiController]
[Route("api/withdrawals")]
[Authorize(Policy = "RequireActiveUserApi")]
public class WithdrawController : ControllerBase
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IWithdrawalService _withdrawalService;
    private readonly IWalletService _walletService;

    public WithdrawController(
        IUserAccountRepository userAccountRepository,
        IWithdrawalService withdrawalService,
        IWalletService walletService)
    {
        _userAccountRepository = userAccountRepository;
        _withdrawalService = withdrawalService;
        _walletService = walletService;
    }

    /// <summary>Rules + current balance for the withdraw form.</summary>
    [HttpGet("info")]
    public async Task<ActionResult<object>> GetInfo(CancellationToken cancellationToken)
    {
        var account = await User.GetUserAccountAsync(_userAccountRepository, cancellationToken);
        if (account == null)
            return Unauthorized(new ApiErrorResponse { Message = "User account not found." });

        var wallet = await _walletService.GetOrCreateWalletAsync(account.Id, cancellationToken);

        return Ok(new
        {
            balance = wallet.Balance,
            minimumAmount = BusinessRules.MinimumWithdrawalAmount,
            minimumLevel = BusinessRules.MinimumWithdrawalLevel,
            accountLevel = account.AccountLevel,
            canWithdraw = account.AccountLevel >= BusinessRules.MinimumWithdrawalLevel
                          && wallet.Balance >= BusinessRules.MinimumWithdrawalAmount
        });
    }

    [HttpGet]
    public async Task<ActionResult<List<WithdrawalDto>>> GetHistory(CancellationToken cancellationToken)
    {
        var account = await User.GetUserAccountAsync(_userAccountRepository, cancellationToken);
        if (account == null)
            return Unauthorized(new ApiErrorResponse { Message = "User account not found." });

        var list = await _withdrawalService.GetUserHistoryAsync(account.Id, cancellationToken);

        return Ok(list.Select(w => new WithdrawalDto
        {
            Id = w.Id,
            Amount = w.Amount,
            Method = w.PaymentMethod.ToString(),
            AccountNumber = Mask(w.AccountNumber),
            Status = w.Status.ToString(),
            Remarks = w.AdminRemarks,
            CreatedAt = w.RequestedAt,
            ProcessedAt = w.ProcessedAt
        }).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<WithdrawalDto>> Submit(
        [FromBody] SubmitWithdrawalRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var account = await User.GetUserAccountAsync(_userAccountRepository, cancellationToken);
        if (account == null)
            return Unauthorized(new ApiErrorResponse { Message = "User account not found." });

        try
        {
            var result = await _withdrawalService.SubmitWithdrawalRequestAsync(
                account.Id,
                request.PaymentMethod,
                request.AccountNumber.Trim(),
                request.Amount,
                cancellationToken);

            return Ok(new WithdrawalDto
            {
                Id = result.Id,
                Amount = result.Amount,
                Method = result.PaymentMethod.ToString(),
                AccountNumber = Mask(result.AccountNumber),
                Status = result.Status.ToString(),
                Remarks = result.AdminRemarks,
                CreatedAt = result.RequestedAt,
                ProcessedAt = result.ProcessedAt
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception)
        {
            return BadRequest(new ApiErrorResponse
            {
                Message = "Withdrawal could not be submitted. Check amount, level, and balance."
            });
        }
    }

    private static string Mask(string accountNumber)
    {
        if (string.IsNullOrEmpty(accountNumber) || accountNumber.Length <= 4)
            return accountNumber;
        return new string('*', accountNumber.Length - 4) + accountNumber[^4..];
    }
}