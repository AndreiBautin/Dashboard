using Microsoft.Extensions.Diagnostics.HealthChecks;
using Dashboard.Infrastructure.Persistence;

namespace Dashboard.Infrastructure.HealthChecks;

/// <summary>
/// Verifies the API can actually open a connection to PostgreSQL. This is
/// deliberately separate from the basic liveness check exposed at /health —
/// liveness must stay dependency-free so it can be asserted in an automated
/// test without a database running. This readiness check is for manual
/// verification during local setup (see README), not the automated test suite.
/// </summary>
public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly DashboardDbContext _dbContext;

    public DatabaseHealthCheck(DashboardDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

        return canConnect
            ? HealthCheckResult.Healthy("Database connection succeeded.")
            : HealthCheckResult.Unhealthy("Could not connect to the database.");
    }
}
