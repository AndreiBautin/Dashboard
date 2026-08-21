using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vantage.Application.Metrics;
using Vantage.Application.Settings;
using Vantage.Application.Social;
using Vantage.Infrastructure.HealthChecks;
using Vantage.Infrastructure.Persistence;
using Vantage.Infrastructure.Persistence.Repositories;

namespace Vantage.Infrastructure;

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
        // VantageDbContext -- by which point the final, fully-merged
        // configuration (real or test) is in place.
        services.AddDbContext<VantageDbContext>((serviceProvider, options) =>
        {
            var connectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString("Vantage")
                ?? throw new InvalidOperationException(
                    "Missing connection string 'Vantage'. Set it via user-secrets " +
                    "(see backend/src/Vantage.Api/README section in the repo README) " +
                    "or the ConnectionStrings__Vantage environment variable.");

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
