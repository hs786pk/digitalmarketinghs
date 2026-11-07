using EarnPlatform.Factories.Interfaces;
using EarnPlatform.Models;
using EarnPlatform.Services.Interfaces;

namespace EarnPlatform.Factories.Implementations;

/// <summary>
/// Concrete Factory implementation. This is where "new UserAccount()" / "new
/// AdminAccount()" actually happens in the entire codebase — services never
/// call these constructors directly. That means default values (account
/// status, referral code generation, timestamps) live in exactly one place.
/// </summary>
public class AccountFactory : IAccountFactory
{
    private readonly IReferralCodeGenerator _referralCodeGenerator;

    public AccountFactory(IReferralCodeGenerator referralCodeGenerator)
    {
        _referralCodeGenerator = referralCodeGenerator;
    }

    public UserAccount CreateUserAccount(string applicationUserId, string firstName, string lastName, string email)
    {
        return new UserAccount
        {
            ApplicationUserId = applicationUserId,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            ReferralCode = _referralCodeGenerator.Generate(),
            AccountLevel = 0,
            AccountStatus = AccountStatus.PendingApproval,
            CreatedAt = DateTime.UtcNow
        };
    }

    public AdminAccount CreateAdminAccount(string applicationUserId, string name, string email, string role = "Admin")
    {
        return new AdminAccount
        {
            ApplicationUserId = applicationUserId,
            Name = name,
            Email = email,
            Role = role,
            CreatedAt = DateTime.UtcNow
        };
    }
}