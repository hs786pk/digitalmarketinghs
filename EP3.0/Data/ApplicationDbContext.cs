using EarnPlatform.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EarnPlatform.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<AdminAccount> AdminAccounts => Set<AdminAccount>();
    public DbSet<Package> Packages => Set<Package>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
    public DbSet<Referral> Referrals => Set<Referral>();
    public DbSet<PaymentApproval> PaymentApprovals => Set<PaymentApproval>();
    public DbSet<WithdrawalRequest> WithdrawalRequests => Set<WithdrawalRequest>();
    public DbSet<WorkTask> WorkTasks => Set<WorkTask>();
    public DbSet<WorkTaskCompletion> WorkTaskCompletions => Set<WorkTaskCompletion>();
    public DbSet<AppSettings> AppSettings => Set<AppSettings>();
    public DbSet<ContactSettings> ContactSettings => Set<ContactSettings>();
    public DbSet<ChannelSettings> ChannelSettings => Set<ChannelSettings>();
    public DbSet<FbrSettings> FbrSettings => Set<FbrSettings>();
    public DbSet<SalarySettings> SalarySettings => Set<SalarySettings>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}