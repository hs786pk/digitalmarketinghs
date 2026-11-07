# EP3.0 Production Deployment Guide

## 1. Runtime requirements
- **.NET**: 10.x (ASP.NET Core)
- **Database**: SQL Server 2019+ (or Azure SQL)
- **OS**: Linux or Windows Server
- **Reverse proxy** (recommended): Nginx, IIS, Apache, or cloud load balancer terminating TLS

## 2. Environment
Set:
```bash
ASPNETCORE_ENVIRONMENT=Production
```

## 3. Required configuration (secrets via environment / secret store — never commit)

| Key | Purpose |
|-----|---------|
| `ConnectionStrings__DefaultConnection` | **Required in Production.** Real SQL Server connection string (LocalDB is rejected) |
| `AllowedHosts` | **Required in Production.** Explicit host name(s), not `*` |
| `DataProtection__KeysPath` | **Required in Production.** Absolute persistent path for DP keys (shared across instances) |
| `ForwardedHeaders__KnownProxies__0` | **Required in Production.** IP of reverse proxy (repeat index for more) |
| `ForwardedHeaders__KnownNetworks__0` | Optional CIDR networks of trusted proxies (e.g. `10.0.0.0/8`) |
| `SeedAdmin__Email` | Initial admin email (seed only if missing) |
| `SeedAdmin__Password` | Initial admin password (strong; seed only) |
| `SeedAdmin__Name` | Optional display name |
| `RunStorageMigration` | Set `true` only for a one-time legacy screenshot migration run |
| `FileUpload__MaxSizeBytes` | Optional override (default 5 MB) |

Example (Linux):
```bash
export ConnectionStrings__DefaultConnection='Server=sql;Database=EarnPlatform;User Id=...;Password=...;Encrypt=True;TrustServerCertificate=False'
export SeedAdmin__Email='admin@example.com'
export SeedAdmin__Password='...'
export DataProtection__KeysPath='/var/earnplatform/dp-keys'
```

## 4. Database migrations
From the app host (or CI):
```bash
dotnet ef database update --project EP3.0/EarnPlatform.csproj
```
Or apply generated SQL scripts in a controlled release process.  
Do **not** auto-apply destructive migrations without review.

## 5. Private file storage
Payment screenshots are stored under:
```
{ContentRoot}/private/payment-screenshots/
```
Requirements:
- **Persistent volume** (not ephemeral container layer)
- Not served as static files / not under `wwwroot`
- App process identity needs read/write
- Back up alongside the database if evidence retention is required

Legacy public path `wwwroot/uploads/payment-screenshots/` is **not** migrated on ordinary startups.
To run the one-time migration (idempotent):

```bash
RunStorageMigration=true ASPNETCORE_ENVIRONMENT=Production dotnet EarnPlatform.dll
# or set "RunStorageMigration": true once, deploy, then set back to false
```

## 6. Data Protection keys
**Production fails to start** if `DataProtection:KeysPath` is missing or empty.  
Set it to a persistent directory shared by every instance (e.g. `/var/earnplatform/dp-keys`).  
Development defaults to `{ContentRoot}/private/dp-keys` when unset.

## 7. HTTPS & reverse proxy
- Terminate TLS at the proxy; forward `X-Forwarded-Proto` and `X-Forwarded-For`.
- **Production fails to start** unless `ForwardedHeaders:KnownProxies` and/or `KnownNetworks` lists the proxy.
- Arbitrary clients cannot inject `X-Forwarded-*` headers.
- Auth cookies use `Secure` + `HttpOnly` + `SameSite=Lax` in Production.

## 8. Health check
```
GET /health
```
Returns overall status including database connectivity. Do not expose detailed diagnostics publicly without an auth gate at the proxy if required by policy.

## 9. Security headers (Production)
Applied by middleware:
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: SAMEORIGIN`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `Permissions-Policy: camera=(), microphone=(), geolocation=()`
- Moderate `Content-Security-Policy` compatible with Blazor Server
- HSTS via `UseHsts()`

## 10. Rate limiting
Login SSR form POSTs to `/` and `/login` are rate-limited: **10 requests / minute / IP**.
Named policy `login` remains available for additional endpoints.

## 11. Backups
Outside the app:
- Automated SQL backups (full + log as appropriate)
- Tested restore procedure
- Retain backups per financial/compliance policy
- Optional: backup `private/payment-screenshots` and `private/dp-keys`

## 12. Deploy steps (typical)
1. Put app offline / drain traffic (optional).
2. Backup database.
3. Deploy new binaries.
4. Run `dotnet ef database update` (or script).
5. Ensure `private/` volume and permissions.
6. Start app with Production env + secrets.
7. Hit `/health`.
8. Smoke test (see below).
9. Restore traffic.

## 13. Rollback
1. Redeploy previous binaries.
2. Reverse DB migration only if the release added a migration **and** rollback SQL is prepared (prefer forward-fix otherwise).
3. Verify `/health` and login.

## 14. Post-deploy smoke test
- [ ] `/health` healthy  
- [ ] User login / logout  
- [ ] Admin login  
- [ ] Unauthorized user cannot open `/admin/*`  
- [ ] Submit payment (screenshot)  
- [ ] Admin views screenshot, approves/rejects  
- [ ] Start + complete task; second complete is no-op  
- [ ] Withdrawal submit / approve / reject  
- [ ] Referral shows after approval  
- [ ] HTTPS cookie flags present  

## 15. Logging
Production default log level is Warning (app Information).  
Do not log passwords, cookies, connection strings, or screenshot contents.

## 16. What this app does *not* store in source
- Production connection strings  
- Admin passwords  
- API keys  
Hardcoded secrets are rejected in review; seed admin comes only from configuration.
