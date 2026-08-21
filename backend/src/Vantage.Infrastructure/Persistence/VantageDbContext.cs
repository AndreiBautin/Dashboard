using Microsoft.EntityFrameworkCore;
using Vantage.Domain.Metrics;
using Vantage.Domain.Settings;
using Vantage.Domain.Social;

namespace Vantage.Infrastructure.Persistence;

/// <summary>
/// The application's single EF Core DbContext. Categories, MetricDefinitions,
/// MonthlySnapshots, and MetricSnapshots arrived in Phase 2; Friends,
/// SocialSnapshots, and AppSettings in Phase 6.
/// </summary>
public sealed class VantageDbContext : DbContext
{
    public VantageDbContext(DbContextOptions<VantageDbContext> options)
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

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VantageDbContext).Assembly);
    }
}
