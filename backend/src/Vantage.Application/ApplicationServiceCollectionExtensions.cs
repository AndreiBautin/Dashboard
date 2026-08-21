using Microsoft.Extensions.DependencyInjection;
using Vantage.Application.Dashboard;
using Vantage.Application.Metrics;
using Vantage.Application.Settings;
using Vantage.Application.Social;
using Vantage.Domain.Metrics;
using Vantage.Domain.Metrics.Evaluators;

namespace Vantage.Application;

/// <summary>
/// Composition root entry point for the Application layer. Vantage.Api calls
/// this once at startup so it never needs to know which use-case services
/// exist — that knowledge lives here, next to the services themselves.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    /// Registers Application-layer services and the Domain evaluators they
    /// depend on. Registering evaluators here (rather than in Domain) keeps
    /// Domain free of any DI framework reference.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IMetricEvaluator, IncreaseMetricEvaluator>();
        services.AddSingleton<IMetricEvaluator, DecreaseMetricEvaluator>();
        services.AddSingleton<IMetricEvaluator, StayAboveMetricEvaluator>();
        services.AddSingleton<IMetricEvaluator, StayBelowMetricEvaluator>();
        services.AddSingleton<IMetricEvaluator, StayWithinRangeMetricEvaluator>();
        services.AddSingleton<MetricEvaluatorFactory>();

        services.AddScoped<MetricEvaluationService>();
        services.AddScoped<MetricTrendService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<CategoryDetailService>();
        services.AddScoped<MetricEntryService>();
        services.AddScoped<SocialService>();
        services.AddScoped<FriendService>();
        services.AddScoped<KeyRelationshipService>();
        services.AddScoped<SettingsService>();

        return services;
    }
}
