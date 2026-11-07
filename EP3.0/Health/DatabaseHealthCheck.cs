using EarnPlatform.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EarnPlatform.Health;

/// <summary>
/// Lightweight readiness probe: can the app open a connection and run a trivial query?
/// Does not expose connection strings or query details in the response.
/// </summary>
public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;

    public DatabaseHealthCheck(IDbContextFactory<ApplicationDbContext> factory) => _factory = factory;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var db = await _factory.CreateDbContextAsync(cancellationToken);
            var canConnect = await db.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("Database reachable.")
                : HealthCheckResult.Unhealthy("Database unreachable.");
        }
        catch (Exception)
        {
            return HealthCheckResult.Unhealthy("Database check failed.");
        }
    }
}