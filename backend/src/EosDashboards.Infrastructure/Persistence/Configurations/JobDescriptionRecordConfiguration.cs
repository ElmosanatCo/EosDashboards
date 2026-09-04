using EosDashboards.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EosDashboards.Infrastructure.Persistence.Configurations;

internal sealed class JobDescriptionRecordConfiguration : IEntityTypeConfiguration<JobDescriptionRecord>
{
    public void Configure(EntityTypeBuilder<JobDescriptionRecord> builder)
    {
        builder.ToTable("JobDescriptionRecords");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasColumnType("bigint").UseIdentityColumn();
        builder.Property(item => item.DepartmentId).HasColumnType("bigint").IsRequired();
        builder.Property(item => item.PersonName).HasMaxLength(200).IsRequired();
        builder.Property(item => item.CreatedAt).HasColumnType("datetime2(3)");
        builder.Property(item => item.UpdatedAt).HasColumnType("datetime2(3)");
        builder.HasIndex(item => new { item.DepartmentId, item.PersonName });
        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(item => item.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(item => item.Versions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
