using EosDashboards.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EosDashboards.Infrastructure.Persistence.Configurations;

internal sealed class SkillCatalogItemConfiguration : IEntityTypeConfiguration<SkillCatalogItem>
{
    public void Configure(EntityTypeBuilder<SkillCatalogItem> builder)
    {
        builder.ToTable("SkillCatalogItems");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasColumnType("bigint").UseIdentityColumn();
        builder.Property(item => item.DepartmentId).HasColumnType("bigint");
        builder.Property(item => item.OwnerDepartmentId).HasColumnType("bigint");
        builder.Property(item => item.Name).HasMaxLength(200).IsRequired();
        builder.Property(item => item.IsActive).IsRequired();
        builder.Property(item => item.CreatedAt).HasColumnType("datetime2(3)");
        builder.Property(item => item.UpdatedAt).HasColumnType("datetime2(3)");
        builder.HasIndex(item => new { item.DepartmentId, item.Name })
            .IsUnique()
            .HasFilter("[DepartmentId] IS NOT NULL");
        builder.HasIndex(item => item.Name)
            .IsUnique()
            .HasFilter("[DepartmentId] IS NULL");
        builder.HasIndex(item => item.DepartmentId);
        builder.HasIndex(item => item.OwnerDepartmentId);
        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(item => item.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(item => item.OwnerDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
