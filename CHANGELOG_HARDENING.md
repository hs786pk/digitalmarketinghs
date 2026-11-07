# EP3.0 Hardening Changelog

## Summary
Remaining production-hardening fixes applied on top of the already-improved EP3.0 codebase.
No business-rule rewrites, no UI redesign, no broad package upgrades.

---

## Priority 1 — Task session validation
- **BusinessRules.IsActiveSession(startedAt, now)**  
  Active only when `startedAt <= now && startedAt >= now - TaskSessionLifetime`.
- **WorkTaskService.IsSessionExpired / IsActiveSession**  
  Future timestamps treated as expired/invalid.
- **WorkTaskCompletionRepository**  
  Count/Get active sessions require `StartedAt <= now`; ExpireStale also expires future-dated Started rows.
- **WalletService.CompleteWorkTaskAsync**  
  Uses the same `BusinessRules.IsActiveSession` helper.

## Priority 2 — Task session edge-case tests
- Expanded `WorkTaskSessionTests` for:
  - exact lifetime boundary (still active)
  - just before / just after expiration
  - future `StartedAt` (not active)
  - `BusinessRules.IsActiveSession` direct coverage

## Priority 3 — Payment amount from server
- `SubmitPaymentAsync` no longer accepts a client `amount`.
- Service loads package and uses `package.Price`.
- `PaymentSubmission.razor` call site updated.

## Priority 4 — Payment account semantics
- Confirmed: `AccountNumber` = platform receiving account; `AccountName` = user sender name.
- Names left unchanged (behaviourally correct; rename would need a migration).

## Priority 5–6 — Referral / team semantics
- `IReferralRepository.CountSuccessfulReferralsAsync` added.
- `TeamStats.ActiveTeamMembers` derived from live Successful referral count (not only the denormalized counter).
- Counter still drives level-up inside the existing approval transaction.

## Priority 7 — Raw status string
- Referral lock query uses `{ReferralStatus.Active.ToString()}` instead of hard-coded `N'Active'`.
- UPDLOCK / ROWLOCK behaviour preserved.

## Priority 8 — Time model
- **New `Common/DisplayTime`** helper: converts UTC → Asia/Karachi for display and business-day filters.
- Replaced all UI `.ToLocalTime()` usages (Admin + User pages).
- Admin “today only” filters use Pakistan business day.
- Persisted event timestamps remain `DateTime.UtcNow` (correct).
- Business-day task limits continue via `IBusinessClock`.

## Priority 14 & 20 — Error handling
- Payment submission: surfaces `InvalidOperationException` messages; generic catch for unexpected errors; `finally` clears `_isSubmitting`.
- Login / Signup: same pattern for business vs unexpected failures.
- Withdraw / WorkView already used try/finally (unchanged).

## Priority 16 — File download authorization
- `/admin/payment-screenshot/{id}` already requires `RequireAdminRole`.
- No public path exposure; verified, no change required.

## Priority 19 — Startup storage migration
- `StorageMigration.MigrateLegacyPaymentScreenshotsAsync` short-circuits when no legacy DB paths and no leftover files remain.
- Still idempotent and safe to re-run.

## Priority 15 — Authorization / ownership
- User pages resolve identity via `ICurrentUserService` / claims (not client-supplied user IDs).
- Task completion checks `completion.UserId == userId`.
- Admin pages use `RequireAdminRole` policy.
- No role-model changes.

---

## Not changed (by design / environment limits)
- Full financial concurrency / double-approval integration tests (would need DB test harness; no new packages added).
- Factory simplification (factories still used for financial/transaction construction).
- Broad dead-code deletion.
- Database migrations (none required for these fixes).
- Package price / referral bonus / withdrawal minimum constants.

---

## Files touched (this pass + previous hardening)
- `Common/BusinessRules.cs`
- `Common/DisplayTime.cs` **(new)**
- `Services/Implementations/WorkTaskService.cs`
- `Services/Implementations/WalletService.cs`
- `Services/Implementations/PaymentApprovalService.cs`
- `Services/Implementations/ReferralService.cs`
- `Services/Interfaces/IPaymentApprovalService.cs`
- `Repositories/Implementations/WorkTaskCompletionRepository.cs`
- `Repositories/Implementations/ReferralRepository.cs`
- `Repositories/Interfaces/IReferralRepository.cs`
- `Data/Seed/StorageMigration.cs`
- `Components/Pages/Auth/PaymentSubmission.razor`
- `Components/Pages/Auth/Login.razor`
- `Components/Pages/Auth/Signup.razor`
- `Components/Pages/Admin/AccountApprovals.razor`
- `Components/Pages/Admin/Users.razor`
- `Components/Pages/Admin/WithdrawalApprovals.razor`
- `Components/Pages/User/Profile.razor`
- `Components/Pages/User/Team.razor`
- `Components/Pages/User/Wallet.razor`
- `EarnPlatform.Tests/WorkTaskSessionTests.cs`
- `EarnPlatform.Tests/ReferralServiceTests.cs`
