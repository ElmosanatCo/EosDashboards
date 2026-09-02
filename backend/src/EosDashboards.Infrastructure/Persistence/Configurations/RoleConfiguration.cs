using EosDashboards.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EosDashboards.Infrastructure.Persistence.Configurations;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(role => role.Id);
        builder.Property(role => role.Id).HasColumnType("bigint").UseIdentityColumn();
        builder.Property(role => role.Code).HasMaxLength(100).IsRequired();
        builder.Property(role => role.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(role => role.CreatedAtUtc).HasColumnType("datetimeoffset(7)");
        builder.HasIndex(role => role.Code).IsUnique();
        builder.HasIndex(role => role.IsActive);
    }
}
