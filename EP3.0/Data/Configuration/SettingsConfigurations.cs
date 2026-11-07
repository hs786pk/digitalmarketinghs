using EarnPlatform.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EarnPlatform.Data.Configurations;

public class AppSettingsConfiguration : IEntityTypeConfiguration<AppSettings>
{
    public void Configure(EntityTypeBuilder<AppSettings> builder)
    {
        // Enforced as a single row at the application layer (SettingsService always uses Id = 1)
    }
}

public class ContactSettingsConfiguration : IEntityTypeConfiguration<ContactSettings>
{
    public void Configure(EntityTypeBuilder<ContactSettings> builder) { }
}

public class ChannelSettingsConfiguration : IEntityTypeConfiguration<ChannelSettings>
{
    public void Configure(EntityTypeBuilder<ChannelSettings> builder) { }
}

public class FbrSettingsConfiguration : IEntityTypeConfiguration<FbrSettings>
{
    public void Configure(EntityTypeBuilder<FbrSettings> builder) { }
}

public class SalarySettingsConfiguration : IEntityTypeConfiguration<SalarySettings>
{
    public void Configure(EntityTypeBuilder<SalarySettings> builder) { }
}