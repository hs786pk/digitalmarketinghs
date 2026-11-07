using EarnPlatform.Api.Dtos;
using EarnPlatform.Api.Helpers;
using EarnPlatform.Data;
using EarnPlatform.Repositories.Interfaces;
using EarnPlatform.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EarnPlatform.Api.Controllers;

[ApiController]
[Route("api/wallet")]
[Authorize(Policy = "RequireActiveUserApi")]
public class WalletController : ControllerBase
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IWalletService _walletService;
    private readonly IWithdrawalService _withdrawalService;
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public WalletController(
        IUserAccountRepository userAccountRepository,
        IWalletService walletService,
        IWithdrawalService withdrawalService,
        IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        _userAccountRepository = userAccountRepository;
        _walletService = walletService;
        _withdrawalService = withdrawalService;
        _dbFactory = dbFactory;
    }

    [HttpGet]
    public async Task<ActionResult<WalletDto>> Get(CancellationToken cancellationToken)
    {
        var account = await User.GetUserAccountAsync(_userAccountRepository, cancellationToken);
        if (account == null)
            return Unauthorized(new ApiErrorResponse { Message = "User account not found." });

        var wallet = await _walletService.GetOrCreateWalletAsync(account.Id, cancellationToken);

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var transactions = await db.WalletTransactions
            .AsNoTracking()
            .Where(t => t.UserId == account.Id)
            .OrderByDescending(t => t.CreatedAt)
            .Take(50)
            .Select(t => new WalletTransactionDto
            {
                Id = t.Id,
                Amount = t.Amount,
                TransactionType = t.TransactionType.ToString(),
                Status = t.Status.ToString(),
                Description = t.Description,
                CreatedAt = t.CreatedAt,
                ApprovedAt = t.ApprovedAt
            })
            .ToListAsync(cancellationToken);

        var withdrawals = await _withdrawalService.GetUserHistoryAsync(account.Id, cancellationToken);

        return Ok(new WalletDto
        {
            Balance = wallet.Balance,
            CurrentEarning = wallet.CurrentEarning,
            ReferralEarning = wallet.ReferralEarning,
            StaffEarning = wallet.StaffEarning,
            ApprovedEarning = wallet.ApprovedEarning,
            PendingEarning = wallet.PendingEarning,
            RejectedEarning = wallet.RejectedEarning,
            UpdatedAt = wallet.UpdatedAt,
            Transactions = transactions,
            Withdrawals = withdrawals.Select(w => new WithdrawalDto
            {
                Id = w.Id,
                Amount = w.Amount,
                Method = w.PaymentMethod.ToString(),
                AccountNumber = MaskAccount(w.AccountNumber),
                Status = w.Status.ToString(),
                Remarks = w.AdminRemarks,
                CreatedAt = w.RequestedAt,
                ProcessedAt = w.ProcessedAt
            }).ToList()
        });
    }

    private static string MaskAccount(string accountNumber)
    {
        if (string.IsNullOrEmpty(accountNumber) || accountNumber.Length <= 4)
            return accountNumber;
        return new string('*', accountNumber.Length - 4) + accountNumber[^4..];
    }
}