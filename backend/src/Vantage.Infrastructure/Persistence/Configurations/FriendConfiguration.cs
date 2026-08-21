using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vantage.Domain.Social;

namespace Vantage.Infrastructure.Persistence.Configurations;

public sealed class FriendConfiguration : IEntityTypeConfiguration<Friend>
{
    public void Configure(EntityTypeBuilder<Friend> builder)
    {
        builder.ToTable("friends");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(f => f.Notes)
            .HasMaxLength(2000);

        builder.Property(f => f.LastHangoutDate)
            .IsRequired();

        builder.Property(f => f.CreatedAt)
            .IsRequired();
    }
}
