# EarnPlatform – Hardened Project

This archive contains the reviewed and hardened version of EarnPlatform.

## Fixes included

- Enforced task daily limits at both task start and final reward-credit time.
- Prevented delayed task sessions from bypassing the daily earning limit.
- Added a 30-minute server-side task-session expiry.
- Revalidated account/package eligibility immediately before task rewards are credited.
- Added Pakistan business-day handling (`Asia/Karachi`) for daily task counters.
- Added a global `RequireActiveUser` authorization policy to user-only pages.
- Moved payment screenshots out of `wwwroot` into private application storage.
- Added an admin-authorized payment screenshot endpoint.
- Added startup migration of legacy public payment screenshots, including orphaned files.
- Added a filtered database unique index for non-rejected payment transaction IDs.
- Made payment rejection atomic and cleared the pending package selection.
- Kept wallet mutations inside the wallet service and transactional financial operations.
- Added security response headers (`nosniff`, `SAMEORIGIN`, strict referrer policy).
- Added the EF Core migration for the hardened task/payment state model.
- Added `StartedAt` and nullable `CompletedAt` to task sessions plus an `Expired` state.
- Added database/index support for task-session lookup and daily-limit checks.
- Removed the obsolete public payment-screenshot project folder configuration.
- Added private upload storage to `.gitignore`.

## Database migration

A new migration is included:

`20260909235000_HardenTaskAndPaymentState`

Apply migrations before running the application against an existing database. For example:

```bash
dotnet ef database update --project EP3.0/EarnPlatform.csproj
```

If your deployment process applies migrations separately, run the migration there instead of enabling automatic production migrations.

## Run

1. Open `EP2.0.slnx` in Visual Studio 2026.
2. Verify `ConnectionStrings:DefaultConnection`.
3. Configure the seed admin credentials using User Secrets or environment variables:
   - `SeedAdmin:Email`
   - `SeedAdmin:Password`
   - optionally `SeedAdmin:Name`
4. Restore packages.
5. Apply EF Core migrations.
6. Build the solution.
7. Test registration, payment submission/approval/rejection, task start/expiry/completion, referral credit, withdrawal submission/approval/rejection, and inactive-user access.

## Validation note

The update environment did not contain the .NET SDK, so `dotnet build` and `dotnet test` could not be executed here. The source, configuration, migration, and project files were checked statically after the changes. A build in Visual Studio/CI is still required before production deployment.
