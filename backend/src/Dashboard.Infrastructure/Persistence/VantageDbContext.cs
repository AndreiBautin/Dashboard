using Microsoft.EntityFrameworkCore;
using Dashboard.Domain.Metrics;
using Dashboard.Domain.Settings;
using Dashboard.Domain.Social;

namespace Dashboard.Infrastructure.Persistence;

/// <summary>
/// The application's single EF Core DbContext. Categories, MetricDefinitions,
/// MonthlySnapshots, and MetricSnapshots arrived in Phase 2; Friends,
/// SocialSnapshots, and AppSettings in Phase 6.
/// </summary>
public sealed class DashboardDbContext : DbContext
{
    public DashboardDbContext(DbContextOptions<DashboardDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<MetricDefinition> MetricDefinitions => Set<MetricDefinition>();

    public DbSet<MonthlySnapshot> MonthlySnapshots => Set<MonthlySnapshot>();

    public DbSet<MetricSnapshot> MetricSnapshots => Set<MetricSnapshot>();

    public DbSet<Friend> Friends => Set<Friend>();

    public DbSet<KeyRelationship> KeyRelationships => Set<KeyRelationship>();

    public DbSet<SocialSnapshot> SocialSnapshots => Set<SocialSnapshot>();

    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DashboardDbContext).Assembly);
    }
}
