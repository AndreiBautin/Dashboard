using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Dashboard.Domain.Social;

namespace Dashboard.Infrastructure.Persistence.Configurations;

public sealed class KeyRelationshipConfiguration : IEntityTypeConfiguration<KeyRelationship>
{
    public void Configure(EntityTypeBuilder<KeyRelationship> builder)
    {
        builder.ToTable("key_relationships");
        builder.HasKey(k => k.Id);

        // Stored as text, not the enum's numeric value -- same rationale as
        // MetricDefinition.EvaluationStrategy: readable in the database and
        // resilient to the enum being reordered later.
        builder.Property(k => k.Kind)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(k => k.LastContactDate)
            .IsRequired();

        builder.Property(k => k.CreatedAt)
            .IsRequired();

        builder.HasIndex(k => k.Kind)
            .IsUnique();
    }
}
