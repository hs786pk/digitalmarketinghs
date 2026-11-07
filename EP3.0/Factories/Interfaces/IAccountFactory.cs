using EarnPlatform.Models;

namespace EarnPlatform.Factories.Interfaces;

public enum AccountType
{
    User,
    Admin
}

/// <summary>
/// Factory Pattern: encapsulates the construction logic for the two account
/// "families" in the system (UserAccount vs AdminAccount). Callers ask for an
/// AccountType and get back a fully-initialized domain object without knowing
/// which concrete class or default field values were used.
/// </summary>
public interface IAccountFactory
{
    /// <summary>
    /// Creates a UserAccount profile object (not yet saved) linked to the given
    /// Identity user, with defaults (PendingApproval status, generated referral
    /// code, etc.) applied consistently regardless of caller.
    /// </summary>
    UserAccount CreateUserAccount(string applicationUserId, string firstName, string lastName, string email);

    /// <summary>
    /// Creates an AdminAccount profile object (not yet saved) linked to the given
    /// Identity user.
    /// </summary>
    AdminAccount CreateAdminAccount(string applicationUserId, string name, string email, string role = "Admin");
}