using EosDashboards.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EosDashboards.Infrastructure.Persistence.Configurations;

internal sealed class JobDescriptionVersionConfiguration : IEntityTypeConfiguration<JobDescriptionVersion>
{
    public void Configure(EntityTypeBuilder<JobDescriptionVersion> builder)
    {
        builder.ToTable("JobDescriptionVersions");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasColumnType("bigint").UseIdentityColumn();
        builder.Property(item => item.PersonName).HasMaxLength(200).IsRequired();
        builder.Property(item => item.DepartmentId).HasColumnType("bigint").IsRequired();
        builder.Property(item => item.PersonnelCode).HasMaxLength(256);
        builder.Property(item => item.Education).HasMaxLength(300).IsRequired();
        builder.Property(item => item.FieldOfStudy).HasMaxLength(300).IsRequired();
        builder.Property(item => item.MinimumExperience).HasMaxLength(200).IsRequired();
        builder.Property(item => item.WorkflowStatus).HasConversion<string>().HasMaxLength(64).IsRequired();
        builder.Property<bool>("_hasCatalogQualityIssues")
            .HasColumnName("HasCatalogQualityIssues")
            .HasDefaultValue(false)
            .IsRequired();
        builder.Property<bool>("_needsReview")
            .HasColumnName("NeedsReview")
            .HasDefaultValue(false)
            .IsRequired();
        builder.Property(item => item.CreatedAt).HasColumnType("datetime2(3)");
        builder.Property(item => item.UpdatedAt).HasColumnType("datetime2(3)");
        builder.Property(item => item.DepartmentApprovedAt).HasColumnType("datetime2(3)");
        builder.Property(item => item.HumanResourcesReviewedAt).HasColumnType("datetime2(3)");
        builder.Property(item => item.RejectionReason).HasMaxLength(2000);
        builder.Property(item => item.ExcelArtifact).HasColumnType("varbinary(max)");
        builder.Property(item => item.ExcelFileName).HasMaxLength(260);
        builder.HasOne(item => item.JobDescriptionRecord)
            .WithMany(item => item.Versions)
            .HasForeignKey(item => item.JobDescriptionRecordId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Ignore(item => item.QualityStatus);
        builder.Navigation(item => item.Tasks).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(item => item.Skills).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(item => item.UnresolvedSkills).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(item => item.UnresolvedTasks).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany(item => item.Tasks)
            .WithOne()
            .HasForeignKey(item => item.JobDescriptionVersionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(item => item.UnresolvedSkills)
            .WithOne()
            .HasForeignKey(item => item.JobDescriptionVersionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(item => item.UnresolvedTasks)
            .WithOne()
            .HasForeignKey(item => item.JobDescriptionVersionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(item => new { item.DepartmentId, item.PersonName });
        builder.HasIndex(item => item.WorkflowStatus);
        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(item => item.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
