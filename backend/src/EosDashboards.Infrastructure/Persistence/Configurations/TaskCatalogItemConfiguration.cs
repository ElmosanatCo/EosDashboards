using EosDashboards.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EosDashboards.Infrastructure.Persistence.Configurations;

internal sealed class TaskCatalogItemConfiguration : IEntityTypeConfiguration<TaskCatalogItem>
{
    public void Configure(EntityTypeBuilder<TaskCatalogItem> builder)
    {
        builder.ToTable("TaskCatalogItems");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasColumnType("bigint").UseIdentityColumn();
        builder.Property(item => item.DepartmentId).HasColumnType("bigint").IsRequired();
        builder.Property(item => item.Title).HasMaxLength(300).IsRequired();
        builder.Property(item => item.IsProject).IsRequired();
        builder.Property(item => item.IsActive).IsRequired();
        builder.Property(item => item.CreatedAt).HasColumnType("datetime2(3)");
        builder.Property(item => item.UpdatedAt).HasColumnType("datetime2(3)");
        builder.HasIndex(item => new { item.DepartmentId, item.Title }).IsUnique();
        builder.HasIndex(item => item.DepartmentId);
        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(item => item.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(item => item.RequiredSkills).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
