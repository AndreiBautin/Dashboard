using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vantage.Domain.Metrics;
using Vantage.Domain.Social;

namespace Vantage.Infrastructure.Persistence.Configurations;

public sealed class MonthlySnapshotConfiguration : IEntityTypeConfiguration<MonthlySnapshot>
{
    public void Configure(EntityTypeBuilder<MonthlySnapshot> builder)
    {
        builder.ToTable("monthly_snapshots");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Month)
            .IsRequired();

        // One review per calendar month — the DB enforces the same
        // invariant the domain model implies.
        builder.HasIndex(s => s.Month).IsUnique();

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.HasMany(s => s.MetricSnapshots)
            .WithOne()
            .HasForeignKey(ms => ms.MonthlySnapshotId)
            .OnDelete(DeleteBehavior.Cascade);

        // MetricSnapshots is a get-only property backed by a private field
        // (MonthlySnapshot is the aggregate root — snapshots are added only
        // through AddMetricSnapshot). EF writes to the field directly.
        builder.Navigation(s => s.MetricSnapshots)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasOne(s => s.SocialSnapshot)
            .WithOne()
            .HasForeignKey<SocialSnapshot>(social => social.MonthlySnapshotId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
