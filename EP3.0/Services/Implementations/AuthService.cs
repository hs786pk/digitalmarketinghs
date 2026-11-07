using EarnPlatform.Common.Exceptions;
using EarnPlatform.Data;
using EarnPlatform.Factories.Interfaces;
using EarnPlatform.Models;
using EarnPlatform.Repositories.Interfaces;
using EarnPlatform.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EarnPlatform.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IAccountFactory _accountFactory;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IReferralCodeGenerator _referralCodeGenerator;
    private readonly IReferralService _referralService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IAccountFactory accountFactory,
        IUserAccountRepository userAccountRepository,
        IReferralCodeGenerator referralCodeGenerator,
        IReferralService referralService,
        ILogger<AuthService> logger)
    {
        _dbFactory = dbFactory;
        _userManager = userManager;
        _signInManager = signInManager;
        _accountFactory = accountFactory;
        _userAccountRepository = userAccountRepository;
        _referralCodeGenerator = referralCodeGenerator;
        _referralService = referralService;
        _logger = logger;
    }

    public async Task<RegisterResult> RegisterUserAsync(
        string firstName,
        string lastName,
        string email,
        string password,
        string? referralCode,
        CancellationToken cancellationToken = default)
    {
        firstName = firstName.Trim();
        lastName = lastName.Trim();
        email = email.Trim();
        referralCode = string.IsNullOrWhiteSpace(referralCode) ? null : referralCode.Trim();

        if (await _userManager.FindByEmailAsync(email) != null)
            return new RegisterResult(false, null, new[] { "This email is already registered." });

        int? referrerUserId = null;
        if (referralCode != null)
        {
            var referrer = await _userAccountRepository.GetByReferralCodeAsync(referralCode, cancellationToken);
            if (referrer == null)
                return new RegisterResult(false, null, new[] { "Invalid referral code." });

            if (referrer.AccountStatus is AccountStatus.Rejected or AccountStatus.Suspended)
                return new RegisterResult(false, null, new[] { "This referral code cannot be used." });

            referrerUserId = referrer.Id;
        }

        var applicationUser = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(applicationUser, password);
        if (!createResult.Succeeded)
        {
            return new RegisterResult(
                false, null,
                createResult.Errors.Select(e => e.Description).ToArray());
        }

        var roleResult = await _userManager.AddToRoleAsync(applicationUser, "User");
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(applicationUser);
            return new RegisterResult(
                false, null,
                roleResult.Errors.Select(e => e.Description).ToArray());
        }

        await using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var userAccount = _accountFactory.CreateUserAccount(
                applicationUser.Id, firstName, lastName, email);

            for (var attempt = 0; attempt < 5; attempt++)
            {
                if (!await context.UserAccounts.AnyAsync(
                        u => u.ReferralCode == userAccount.ReferralCode, cancellationToken))
                    break;
                userAccount.ReferralCode = _referralCodeGenerator.Generate();
            }

            if (referrerUserId.HasValue)
                userAccount.ReferredByUserId = referrerUserId;

            await context.UserAccounts.AddAsync(userAccount, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            if (referrerUserId.HasValue)
            {
                await context.Referrals.AddAsync(new Referral
                {
                    ReferrerUserId = referrerUserId.Value,
                    ReferredUserId = userAccount.Id,
                    Status = ReferralStatus.Active,
                    CreatedAt = DateTime.UtcNow
                }, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return new RegisterResult(true, applicationUser.Id, Array.Empty<string>());
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            await _userManager.DeleteAsync(applicationUser);
            _logger.LogError(ex, "Database error while registering {Email}.", email);

            if (ex.InnerException?.Message.Contains("IX_UserAccounts_Email", StringComparison.OrdinalIgnoreCase) == true
                || ex.InnerException?.Message.Contains("IX_AspNetUsers_NormalizedEmail", StringComparison.OrdinalIgnoreCase) == true)
            {
                return new RegisterResult(false, null, new[] { "This email is already registered." });
            }

            return new RegisterResult(false, null, new[] { "Registration could not be completed. Please try again." });
        }
        catch (AppException)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            await _userManager.DeleteAsync(applicationUser);
            throw;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            await _userManager.DeleteAsync(applicationUser);
            _logger.LogError(ex, "Registration failed for {Email}.", email);
            return new RegisterResult(false, null, new[] { "Registration could not be completed. Please try again." });
        }
    }

    public async Task<LoginResult> LoginAsync(
        string email,
        string password,
        bool rememberMe,
        CancellationToken cancellationToken = default)
    {
        var applicationUser = await _userManager.FindByEmailAsync(email);
        if (applicationUser == null)
            return new LoginResult(false, false, "Invalid email or password.");

        var result = await _signInManager.PasswordSignInAsync(
            applicationUser, password, rememberMe, lockoutOnFailure: true);

        if (result.IsLockedOut)
            return new LoginResult(false, true,
                "Your account is temporarily locked due to multiple failed attempts. Please try again later.");

        if (!result.Succeeded)
            return new LoginResult(false, false, "Invalid email or password.");

        var userAccount = await _userAccountRepository.GetByApplicationUserIdAsync(
            applicationUser.Id, cancellationToken);

        if (userAccount != null)
        {
            userAccount.LastLoginAt = DateTime.UtcNow;
            await _userAccountRepository.UpdateAsync(userAccount, cancellationToken);
        }

        var isAdmin = await _userManager.IsInRoleAsync(applicationUser, "Admin");

        return new LoginResult(
            true, false, null, isAdmin,
            userAccount?.AccountStatus,
            userAccount?.SelectedPackageId);
    }

    public Task LogoutAsync() => _signInManager.SignOutAsync();
}