using Microsoft.Extensions.DependencyInjection;
using Dashboard.Application;
using Dashboard.Application.Dashboard;
using Dashboard.Application.Metrics;
using Dashboard.Application.Settings;
using Dashboard.Application.Social;

namespace Dashboard.Demo;

/// <summary>
/// A fully wired application, running entirely in memory.
///
/// The important line in this file is <c>services.AddApplication()</c>: the
/// demo composes itself through the *same* composition root the ASP.NET Core
/// API uses, so the services it resolves are the real ones, registered the
/// real way, with the real evaluator set behind them. The only substitution
/// is the persistence layer — the seven repository interfaces and
/// <see cref="IUnitOfWork"/> are bound to the in-memory implementations
/// instead of the EF Core ones.
///
/// That is what makes the deployed demo trustworthy as a portfolio artifact:
/// it is not a mock of the application, it is the application with a
/// different database.
/// </summary>
public sealed class DemoWorkspace
{
    private readonly ServiceProvider _serviceProvider;

    public DemoStore Store { get; }

    private DemoWorkspace(DemoStore store, ServiceProvider serviceProvider)
    {
        Store = store;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Builds the workspace and fills it from the fixture. Uses
    /// <see cref="DemoSeeder.FillIfEmpty"/> rather than the destructive
    /// variant even though the store is provably new here, so that the only
    /// call site in the startup path is the non-destructive one.
    /// </summary>
    public static DemoWorkspace Create(DateOnly today)
    {
        var store = new DemoStore();
        DemoSeeder.FillIfEmpty(store, today);

        var services = new ServiceCollection();
        services.AddApplication();

        services.AddSingleton(store);
        services.AddScoped<ICategoryRepository, InMemoryCategoryRepository>();
        services.AddScoped<IMetricDefinitionRepository, InMemoryMetricDefinitionRepository>();
        services.AddScoped<IMonthlySnapshotRepository, InMemoryMonthlySnapshotRepository>();
        services.AddScoped<IMetricSnapshotRepository, InMemoryMetricSnapshotRepository>();
        services.AddScoped<IFriendRepository, InMemoryFriendRepository>();
        services.AddScoped<IKeyRelationshipRepository, InMemoryKeyRelationshipRepository>();
        services.AddScoped<IAppSettingRepository, InMemoryAppSettingRepository>();
        services.AddScoped<IUnitOfWork, InMemoryUnitOfWork>();

        return new DemoWorkspace(store, services.BuildServiceProvider());
    }

    /// <summary>
    /// Throws away the current data and reseeds from the fixture. Backs the
    /// demo's "reset" control. Safe here and nowhere else: this store holds
    /// only generated fixture data, by construction.
    /// </summary>
    public void Reset(DateOnly today) => DemoSeeder.ResetAndFill(Store, today);

    public DashboardService Dashboard => Resolve<DashboardService>();

    public CategoryDetailService CategoryDetail => Resolve<CategoryDetailService>();

    public MetricTrendService MetricTrend => Resolve<MetricTrendService>();

    public MetricEntryService MetricEntry => Resolve<MetricEntryService>();

    public SocialService Social => Resolve<SocialService>();

    public FriendService Friends => Resolve<FriendService>();

    public KeyRelationshipService KeyRelationships => Resolve<KeyRelationshipService>();

    public SettingsService Settings => Resolve<SettingsService>();

    public ICategoryRepository CategoryRepository => Resolve<ICategoryRepository>();

    /// <remarks>
    /// The application's services are registered <c>Scoped</c> because on the
    /// server one scope means one HTTP request. In the browser there is no
    /// request to scope to and exactly one user, so resolving from the root
    /// provider — a single, process-long scope — is the honest equivalent
    /// rather than a shortcut. It also means the in-memory store behaves like
    /// a database that stays connected, which is precisely what it is.
    /// </remarks>
    private T Resolve<T>() where T : notnull => _serviceProvider.GetRequiredService<T>();
}
