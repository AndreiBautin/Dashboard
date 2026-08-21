using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vantage.Domain.Metrics;

namespace Vantage.Infrastructure.Persistence.Configurations;

public sealed class MetricDefinitionConfiguration : IEntityTypeConfiguration<MetricDefinition>
{
    public void Configure(EntityTypeBuilder<MetricDefinition> builder)
    {
        builder.ToTable("metric_definitions");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.Unit)
            .IsRequired()
            .HasMaxLength(50);

        // Stored as text, not the enum's numeric value — readable in the
        // database and resilient to the enum being reordered later.
        builder.Property(m => m.EvaluationStrategy)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // EvaluationConfig is a plain record in Domain with no knowledge of
        // JSON — the serialization concern lives entirely here.
        builder.Property(m => m.EvaluationConfig)
            .HasConversion(
                config => JsonSerializer.Serialize(config, JsonSerializerOptions.Web),
                json => JsonSerializer.Deserialize<EvaluationConfig>(json, JsonSerializerOptions.Web)!)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(m => m.SortOrder)
            .IsRequired();

        builder.Property(m => m.IsActive)
            .IsRequired();

        builder.Property(m => m.IsCalculated)
            .IsRequired();

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(m => m.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
