using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Dashboard.Application.Metrics;
using Dashboard.Application.Settings;
using Dashboard.Application.Social;
using Dashboard.Infrastructure.HealthChecks;
using Dashboard.Infrastructure.Persistence;
using Dashboard.Infrastructure.Persistence.Repositories;

namespace Dashboard.Infrastructure;

/// <summary>
/// Composition root entry point for the Infrastructure layer — the only
/// place that knows about EF Core, Npgsql, or the connection string. Api's
/// Program.cs calls AddInfrastructure once and never touches these
/// concerns directly.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Deliberately deferred into the options callback rather than read
        // eagerly here: this method runs synchronously as part of Program.cs's
        // top-level statements, before WebApplicationFactory-based tests get a
        // chance to layer in their own configuration/DbContext overrides. An
        // eager throw here would fire before those overrides ever apply. The
        // callback form runs lazily, only when a scope actually resolves
        // DashboardDbContext -- by which point the final, fully-merged
        // configuration (real or test) is in place.
        services.AddDbContext<DashboardDbContext>((serviceProvider, options) =>
        {
            var connectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString("Dashboard")
                ?? throw new InvalidOperationException(
                    "Missing connection string 'Dashboard'. Set it via user-secrets " +
                    "(see backend/src/Dashboard.Api/README section in the repo README) " +
                    "or the ConnectionStrings__Dashboard environment variable.");

            options.UseNpgsql(connectionString);
        });

        services
            .AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

        services.AddScoped<ICategoryRepository, EfCategoryRepository>();
        services.AddScoped<IMetricDefinitionRepository, EfMetricDefinitionRepository>();
        services.AddScoped<IMonthlySnapshotRepository, EfMonthlySnapshotRepository>();
        services.AddScoped<IMetricSnapshotRepository, EfMetricSnapshotRepository>();
        services.AddScoped<IFriendRepository, EfFriendRepository>();
        services.AddScoped<IKeyRelationshipRepository, EfKeyRelationshipRepository>();
        services.AddScoped<IAppSettingRepository, EfAppSettingRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        return services;
    }
}
