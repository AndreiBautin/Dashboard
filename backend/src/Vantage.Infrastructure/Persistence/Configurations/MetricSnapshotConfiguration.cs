using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vantage.Domain.Metrics;

namespace Vantage.Infrastructure.Persistence.Configurations;

public sealed class MetricSnapshotConfiguration : IEntityTypeConfiguration<MetricSnapshot>
{
    public void Configure(EntityTypeBuilder<MetricSnapshot> builder)
    {
        builder.ToTable("metric_snapshots");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Value)
            .HasColumnType("numeric(14,2)")
            .IsRequired();

        builder.Property(s => s.RecordedAt)
            .IsRequired();

        builder.HasOne<MetricDefinition>()
            .WithMany()
            .HasForeignKey(s => s.MetricDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        // One value per metric per monthly review — not two entries for the
        // same metric in the same month.
        builder.HasIndex(s => new { s.MetricDefinitionId, s.MonthlySnapshotId }).IsUnique();
    }
}
