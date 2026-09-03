using EosDashboards.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EosDashboards.Infrastructure.Persistence.Configurations;

internal sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");
        builder.HasKey(department => department.Id);
        builder.Property(department => department.Id).HasColumnType("bigint").UseIdentityColumn();
        builder.Property(department => department.Name).HasMaxLength(200).IsRequired();
        builder.Property(department => department.ParentDepartmentId).HasColumnType("bigint");
        builder.Property(department => department.CreatedAt).HasColumnType("datetime2(3)");
        builder.Property(department => department.UpdatedAt).HasColumnType("datetime2(3)");
        builder.Property(department => department.RowVersion).IsRequired().IsRowVersion().HasColumnType("rowversion");
        builder.HasOne(department => department.ParentDepartment)
            .WithMany()
            .HasForeignKey(department => department.ParentDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(department => department.ParentDepartmentId);
        builder.HasIndex(department => department.Name).IsUnique();
    }
}
