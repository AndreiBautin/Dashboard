using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vantage.Domain.Settings;

namespace Vantage.Infrastructure.Persistence.Configurations;

public sealed class AppSettingConfiguration : IEntityTypeConfiguration<AppSetting>
{
    public void Configure(EntityTypeBuilder<AppSetting> builder)
    {
        builder.ToTable("app_settings");
        builder.HasKey(s => s.Key);

        builder.Property(s => s.Key)
            .HasMaxLength(200);

        builder.Property(s => s.Value)
            .IsRequired()
            .HasMaxLength(2000);
    }
}
