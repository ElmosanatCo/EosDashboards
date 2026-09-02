using EosDashboards.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EosDashboards.Infrastructure.Persistence.Configurations;

internal sealed class UserPreferenceConfiguration : IEntityTypeConfiguration<UserPreference>
{
    public void Configure(EntityTypeBuilder<UserPreference> builder)
    {
        builder.ToTable("UserPreferences");
        builder.HasKey(preference => preference.Id);
        builder.Property(preference => preference.Id).HasColumnType("bigint").UseIdentityColumn();
        builder.Property(preference => preference.UserId).HasColumnType("bigint");
        builder.Property(preference => preference.AppearanceMode).HasMaxLength(16).IsRequired();
        builder.Property(preference => preference.Palette).HasMaxLength(64).IsRequired();
        builder.Property(preference => preference.CreatedAtUtc).HasColumnType("datetimeoffset(7)");
        builder.Property(preference => preference.UpdatedAtUtc).HasColumnType("datetimeoffset(7)");
        builder.HasIndex(preference => preference.UserId).IsUnique();
        builder.HasOne<User>()
            .WithOne()
            .HasForeignKey<UserPreference>(preference => preference.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
