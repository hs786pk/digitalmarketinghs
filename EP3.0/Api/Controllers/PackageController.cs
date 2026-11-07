using EarnPlatform.Api.Dtos;
using EarnPlatform.Api.Helpers;
using EarnPlatform.Repositories.Interfaces;
using EarnPlatform.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EarnPlatform.Api.Controllers;

[ApiController]
[Route("api/packages")]
[Authorize(Policy = "RequireAuthenticatedApi")]
public class PackageController : ControllerBase
{
    private readonly IPackageService _packageService;
    private readonly ISettingsService _settingsService;
    private readonly IPaymentApprovalService _paymentService;
    private readonly IUserAccountRepository _users;
    private readonly IFileUploadService _fileUpload;

    public PackageController(
        IPackageService packageService,
        ISettingsService settingsService,
        IPaymentApprovalService paymentService,
        IUserAccountRepository users,
        IFileUploadService fileUpload)
    {
        _packageService = packageService;
        _settingsService = settingsService;
        _paymentService = paymentService;
        _users = users;
        _fileUpload = fileUpload;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var packages = await _packageService.GetActivePackagesAsync(ct);
        var list = packages.Select(p => new
        {
            id = p.Id,
            name = p.Name,
            price = p.Price,
            level = p.Level,
            dailyAdLimit = p.DailyAdLimit,
            description = p.Description
        }).ToList();

        return Ok(list);
    }

    [HttpGet("payment-info")]
    public async Task<IActionResult> PaymentInfo([FromQuery] int packageId, CancellationToken ct)
    {
        var package = await _packageService.GetByIdAsync(packageId, ct);
        if (package == null || !package.IsActive)
            return NotFound(new ApiErrorResponse { Message = "Package not found." });

        var settings = await _settingsService.GetPaymentSettingsAsync(ct);

        return Ok(new
        {
            packageId = package.Id,
            packageName = package.Name,
            amount = package.Price,
            paymentAccountName = settings?.PaymentAccountName ?? "",
            paymentAccountNumber = settings?.PaymentAccountNumber ?? ""
        });
    }

    [HttpPost("payment")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> SubmitPayment(
        [FromForm] int packageId,
        [FromForm] string accountName,
        [FromForm] string transactionId,
        IFormFile screenshot,
        CancellationToken ct)
    {
        if (screenshot == null || screenshot.Length == 0)
            return BadRequest(new ApiErrorResponse { Message = "Payment screenshot is required." });

        if (string.IsNullOrWhiteSpace(accountName) || string.IsNullOrWhiteSpace(transactionId))
            return BadRequest(new ApiErrorResponse { Message = "Account name and transaction ID are required." });

        var account = await User.GetUserAccountAsync(_users, ct);
        if (account == null)
            return Unauthorized(new ApiErrorResponse { Message = "User account not found." });

        try
        {
            await using var stream = screenshot.OpenReadStream();
            var upload = await _fileUpload.SavePaymentScreenshotAsync(stream, screenshot.FileName, screenshot.Length);
            if (!upload.Succeeded)
                return BadRequest(new ApiErrorResponse { Message = upload.ErrorMessage ?? "Upload failed." });

            await _paymentService.SubmitPaymentAsync(
                account.Id,
                packageId,
                accountName.Trim(),
                transactionId.Trim(),
                upload.RelativePath!,
                ct);

            return Ok(new { message = "Payment submitted. Waiting for admin approval." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
        catch
        {
            return BadRequest(new ApiErrorResponse { Message = "Could not submit payment. Please try again." });
        }
    }
}