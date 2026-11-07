using EarnPlatform.Api.Filters;
using EarnPlatform.Api.Services;
using EarnPlatform.Authorization;
using EarnPlatform.Components;
using EarnPlatform.Data;
using EarnPlatform.Factories.Implementations;
using EarnPlatform.Factories.Interfaces;
using EarnPlatform.Repositories.Implementations;
using EarnPlatform.Repositories.Interfaces;
using EarnPlatform.Services.Implementations;
using EarnPlatform.Services.Interfaces;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Text;
using System.Threading.RateLimiting;
using EarnPlatform.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// When false, the app will not redirect HTTP→HTTPS or enable HSTS.
// Required on hosts with no SSL certificate. Prefer true once SSL is available.
// When false, the app will not redirect HTTP→HTTPS or enable HSTS.
// Development may run on plain HTTP. Production always requires HTTPS.
var requireHttps = builder.Configuration.GetValue("Security:RequireHttps", defaultValue: false);

// ---------- Production configuration guards ----------
if (builder.Environment.IsProduction())
{
    // HTTPS is mandatory in Production — config cannot disable it.
    requireHttps = true;

    var allowedHosts = builder.Configuration["AllowedHosts"];
    if (string.IsNullOrWhiteSpace(allowedHosts) || allowedHosts.Trim() == "*")
    {
        throw new InvalidOperationException(
            "Production requires an explicit AllowedHosts value (comma/semicolon-separated host names). " +
            "Do not use '*'.");
    }

    var dpKeysPath = builder.Configuration["DataProtection:KeysPath"];
    if (string.IsNullOrWhiteSpace(dpKeysPath))
    {
        throw new InvalidOperationException(
            "Production requires DataProtection:KeysPath to be set to a persistent directory " +
            "shared by all application instances (e.g. /var/earnplatform/dp-keys).");
    }
}

// ---------- Repository registrations ----------
builder.Services.AddScoped<IUserAccountRepository, UserAccountRepository>();
builder.Services.AddScoped<IAdminAccountRepository, AdminAccountRepository>();
builder.Services.AddScoped<IWalletRepository, WalletRepository>();
builder.Services.AddScoped<IPackageRepository, PackageRepository>();
builder.Services.AddScoped<IPaymentApprovalRepository, PaymentApprovalRepository>();
builder.Services.AddScoped<IWithdrawalRequestRepository, WithdrawalRequestRepository>();
builder.Services.AddScoped<IWorkTaskRepository, WorkTaskRepository>();
builder.Services.AddScoped<IWorkTaskCompletionRepository, WorkTaskCompletionRepository>();
builder.Services.AddScoped<IReferralRepository, ReferralRepository>();
builder.Services.AddScoped<ISettingsRepository, SettingsRepository>();

// ---------- Blazor / Razor Components ----------
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();

// ---------- JWT for mobile API (does not replace cookie auth for Blazor) ----------
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
{
    if (builder.Environment.IsProduction())
        throw new InvalidOperationException("Production requires Jwt:Key (at least 32 characters).");
    // Dev fallback so the site still runs if Jwt is not set yet
    jwtKey = "DEV_ONLY_CHANGE_ME_TO_A_LONG_SECRET_KEY_32+";
}

builder.Services.AddAuthentication()
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "https://digital-marketingpk.com",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "EarnPlatform.Mobile",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ApiExceptionFilter>();
});

// ---------- Database (factory for short-lived contexts — required for Blazor Server) ----------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

if (builder.Environment.IsProduction())
{
    if (connectionString.Contains("localdb", StringComparison.OrdinalIgnoreCase)
        || connectionString.Contains("(localdb)", StringComparison.OrdinalIgnoreCase)
        || connectionString.Contains("Data Source=:memory:", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException(
            "Production must use a real SQL Server connection string. " +
            "LocalDB / in-memory connection strings are not allowed. " +
            "Set ConnectionStrings__DefaultConnection via environment or secret store.");
    }
}

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Keep a scoped DbContext as well so Identity / health checks that expect
// ApplicationDbContext via DI continue to work. The scoped instance is
// created from the factory and lives only for the current scope/request.
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDatabaseDeveloperPageExceptionFilter();
}
// ---------- Data Protection (persist keys across restarts / multiple instances) ----------
// Production already required an explicit path above (absolute OR relative to content root).
// Relative values like "private/dp-keys" resolve under ContentRootPath (correct for shared Windows hosts).
var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
if (string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, "private", "dp-keys");
}
else if (!Path.IsPathRooted(dataProtectionKeysPath))
{
    dataProtectionKeysPath = Path.GetFullPath(
        Path.Combine(builder.Environment.ContentRootPath, dataProtectionKeysPath));
}
else
{
    dataProtectionKeysPath = Path.GetFullPath(dataProtectionKeysPath);
}
try
{
    Directory.CreateDirectory(dataProtectionKeysPath);
}
catch (UnauthorizedAccessException ex)
{
    throw new InvalidOperationException(
        $"Cannot create or access Data Protection keys directory '{dataProtectionKeysPath}'. " +
        "On Windows shared hosting set DataProtection:KeysPath to a relative path under the site " +
        "(e.g. private/dp-keys) and ensure that folder exists and is writable by the app pool. " +
        $"Underlying error: {ex.Message}", ex);
}
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
    .SetApplicationName("EarnPlatform");

// ---------- Forwarded headers (only trust configured proxies) ----------
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();

    var knownProxies = builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>()
        ?? Array.Empty<string>();
    foreach (var proxy in knownProxies)
    {
        if (IPAddress.TryParse(proxy.Trim(), out var ip))
            options.KnownProxies.Add(ip);
    }

    var knownNetworks = builder.Configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>()
        ?? Array.Empty<string>();
    foreach (var cidr in knownNetworks)
    {
        if (TryParseCidr(cidr, out var network))
            options.KnownIPNetworks.Add(network);
    }

    // In Production, refuse to honor X-Forwarded-* from arbitrary clients.
    // Operators must list the reverse-proxy address(es). Development may run without a proxy.
    if (builder.Environment.IsProduction()
        && options.KnownProxies.Count == 0
        && options.KnownIPNetworks.Count == 0)
    {
        throw new InvalidOperationException(
            "Production requires ForwardedHeaders:KnownProxies and/or ForwardedHeaders:KnownNetworks " +
            "so X-Forwarded-* headers are only accepted from trusted reverse proxies.");
    }
});

// ---------- Identity ----------
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/access-denied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.Name = ".EarnPlatform.Auth";
    options.Cookie.SameSite = SameSiteMode.Lax;
    // Secure cookies only when HTTPS is required; otherwise login breaks on pure HTTP.
    options.Cookie.SecurePolicy = requireHttps
        ? CookieSecurePolicy.Always
        : CookieSecurePolicy.SameAsRequest;
});

// Antiforgery cookie must also work over HTTP when SSL is not available.
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SecurePolicy = requireHttps
        ? CookieSecurePolicy.Always
        : CookieSecurePolicy.SameAsRequest;
});

builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, ApiAuthorizationResultHandler>();

builder.Services.AddScoped<IAuthorizationHandler, ActiveUserHandler>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));

    // Any authenticated mobile user (Pending allowed — onboarding / profile / me)
    options.AddPolicy("RequireAuthenticatedApi", policy =>
    {
        policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
    });

    // Authenticated + domain AccountStatus.Active (earning / money)
    options.AddPolicy("RequireActiveUserApi", policy =>
    {
        policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
        policy.RequireRole("User");
        policy.AddRequirements(new ActiveUserRequirement());
    });

    // Admin-only API (when you add admin mobile/API endpoints)
    options.AddPolicy("RequireAdminApi", policy =>
    {
        policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Admin");
    });

    // Web Blazor (cookie)
    options.AddPolicy("RequireUserRole", policy => policy.RequireRole("User"));
    options.AddPolicy("RequireActiveUser", policy =>
    {
        policy.RequireRole("User");
        policy.AddRequirements(new ActiveUserRequirement());
    });
});
// ---------- Rate limiting ----------
// Global limiter applies the "login" partition to SSR login form posts (/ and /login).
// Named policy remains available for other endpoints that opt in.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        if (!HttpMethods.IsPost(httpContext.Request.Method))
            return RateLimitPartition.GetNoLimiter("none");

        var path = httpContext.Request.Path.Value ?? string.Empty;
        // Login is a static SSR page at "/" and "/login" (ExcludeFromInteractiveRouting).
        if (path.Equals("/login", StringComparison.OrdinalIgnoreCase))
        {
            var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: "login:" + ip,
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
        }

        return RateLimitPartition.GetNoLimiter("none");
    });
});

// ---------- Health checks ----------
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy())
    .AddCheck<EarnPlatform.Health.DatabaseHealthCheck>("database");

// ---------- Factory Pattern registrations ----------
builder.Services.AddScoped<IAccountFactory, AccountFactory>();
builder.Services.AddScoped<IPackageFactory, PackageFactory>();
builder.Services.AddScoped<IWorkTaskFactory, WorkTaskFactory>();
builder.Services.AddScoped<IWalletTransactionFactory, WalletTransactionFactory>();
builder.Services.AddScoped<IDashboardCardFactory, DashboardCardFactory>();
builder.Services.AddScoped<IWithdrawalProcessorFactory, WithdrawalProcessorFactory>();
builder.Services.AddScoped<IReferralCodeGenerator, ReferralCodeGenerator>();

// ---------- Service registrations ----------
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<IPackageService, PackageService>();
builder.Services.AddScoped<IWorkTaskService, WorkTaskService>();
builder.Services.AddScoped<IPaymentApprovalService, PaymentApprovalService>();
builder.Services.AddScoped<IWithdrawalService, WithdrawalService>();
builder.Services.AddScoped<IReferralService, ReferralService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddSingleton<IBusinessClock, BusinessClock>();
builder.Services.AddScoped<IUserDashboardService, UserDashboardService>();

builder.Services.Configure<FileUploadOptions>(builder.Configuration.GetSection("FileUpload"));
builder.Services.AddScoped<IFileUploadService, FileUploadService>();

var app = builder.Build();



// ---------- Seed roles / packages (always). Legacy file migration is opt-in. ----------
using (var scope = app.Services.CreateScope())
{
    await EarnPlatform.Data.Seed.IdentitySeeder.SeedAsync(scope.ServiceProvider);
    await EarnPlatform.Data.Seed.DataSeeder.SeedAsync(scope.ServiceProvider);

    // One-time legacy screenshot migration: only when explicitly requested
    // (env RunStorageMigration=true or config RunStorageMigration=true).
    // Do not run on every ordinary production startup.
    var runStorageMigration =
        app.Configuration.GetValue<bool>("RunStorageMigration")
        || string.Equals(Environment.GetEnvironmentVariable("RunStorageMigration"), "true", StringComparison.OrdinalIgnoreCase);

    if (runStorageMigration)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
        logger.LogInformation("RunStorageMigration is enabled — migrating legacy payment screenshots.");
        await EarnPlatform.Data.Seed.StorageMigration.MigrateLegacyPaymentScreenshotsAsync(scope.ServiceProvider);
        logger.LogInformation("Legacy payment screenshot migration finished.");
    }
}

// ---------- HTTP request pipeline ----------
// Only meaningful when KnownProxies/Networks are configured (enforced in Production).
app.UseForwardedHeaders();
app.UseCorrelationId();


if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    if (requireHttps)
        app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
if (requireHttps)
    app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    if (!app.Environment.IsDevelopment())
    {
        // Allow jsDelivr only for Bootstrap Icons CSS/font (see App.razor).
        context.Response.Headers["Content-Security-Policy"] =
    "default-src 'self'; " +
    "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
    "style-src 'self' 'unsafe-inline'; " +
    "img-src 'self' data: blob:; " +
    "font-src 'self' data:; " +
    "connect-src 'self' ws: wss:; " +
    "frame-ancestors 'self'; " +
    "base-uri 'self'; " +
    "form-action 'self'";
    }
    await next();
});


app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseAntiforgery();

app.MapStaticAssets();
app.UseStaticFiles();

// Logout: classic form POST. Explicit antiforgery validation so a cross-site
// form cannot force-log-out a user. Forms must include <AntiforgeryToken />.
app.MapPost("/account/logout", async (HttpContext httpContext, IAntiforgery antiforgery, IAuthService authService) =>
{
    await antiforgery.ValidateRequestAsync(httpContext);
    await authService.LogoutAsync();
    return Results.Redirect("/login");
});

app.MapGet("/admin/payment-screenshot/{id:int}", async (int id, ApplicationDbContext db, IFileUploadService files) =>
{
    var approval = await db.PaymentApprovals.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
    if (approval?.Status != EarnPlatform.Models.PaymentApprovalStatus.Pending || string.IsNullOrWhiteSpace(approval.PaymentScreenshotPath))
        return Results.NotFound();

    var stream = files.OpenPrivateFile(approval.PaymentScreenshotPath);
    if (stream == null) return Results.NotFound();
    var extension = Path.GetExtension(approval.PaymentScreenshotPath).ToLowerInvariant();
    var contentType = extension switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".webp" => "image/webp",
        _ => "application/octet-stream"
    };
    return Results.File(stream, contentType);
}).RequireAuthorization("RequireAdminRole");

app.MapHealthChecks("/health");
app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// ---------- helpers ----------
static bool TryParseCidr(string cidr, out System.Net.IPNetwork network)
{
    network = default!;
    if (string.IsNullOrWhiteSpace(cidr)) return false;
    cidr = cidr.Trim();
    try
    {
        // System.Net.IPNetwork is available on .NET 5+ (Parse accepts "10.0.0.0/8").
        network = System.Net.IPNetwork.Parse(cidr);
        return true;
    }
    catch
    {
        return false;
    }
}