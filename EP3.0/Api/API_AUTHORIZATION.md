# API authorization matrix

| Endpoint | Auth |
|----------|------|
| POST /api/auth/register | Anonymous + rate limit |
| POST /api/auth/login | Anonymous + rate limit |
| POST /api/auth/refresh | Anonymous + rate limit |
| POST /api/auth/logout | JWT (RequireAuthenticatedApi) |
| GET  /api/auth/me | JWT (RequireAuthenticatedApi) |
| /api/packages/* | JWT (RequireAuthenticatedApi) — Pending allowed |
| /api/profile | JWT (RequireAuthenticatedApi) |
| /api/info/* | JWT (RequireAuthenticatedApi) |
| /api/dashboard | JWT + Active (RequireActiveUserApi) |
| /api/wallet | JWT + Active |
| /api/work/* | JWT + Active |
| /api/withdrawals/* | JWT + Active |
| /api/team | JWT + Active |
| /health | Public |

Active = domain AccountStatus.Active (not only a valid JWT).