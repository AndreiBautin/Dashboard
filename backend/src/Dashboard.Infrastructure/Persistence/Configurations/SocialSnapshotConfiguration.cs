using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Dashboard.Domain.Social;

namespace Dashboard.Infrastructure.Persistence.Configurations;

public sealed class SocialSnapshotConfiguration : IEntityTypeConfiguration<SocialSnapshot>
{
    public void Configure(EntityTypeBuilder<SocialSnapshot> builder)
    {
        builder.ToTable("social_snapshots");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.ActiveFriendCount)
            .IsRequired();

        // One SocialSnapshot per MonthlySnapshot -- same one-per-month
        // invariant MonthlySnapshotConfiguration enforces for the aggregate
        // itself. The relationship itself is configured from the
        // MonthlySnapshot side (see MonthlySnapshotConfiguration).
        builder.HasIndex(s => s.MonthlySnapshotId).IsUnique();
    }
}
