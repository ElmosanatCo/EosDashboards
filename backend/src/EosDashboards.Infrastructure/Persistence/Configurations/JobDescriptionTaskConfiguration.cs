using EosDashboards.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EosDashboards.Infrastructure.Persistence.Configurations;

internal sealed class JobDescriptionTaskConfiguration : IEntityTypeConfiguration<JobDescriptionTask>
{
    public void Configure(EntityTypeBuilder<JobDescriptionTask> builder)
    {
        builder.ToTable("JobDescriptionTasks");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasColumnType("bigint").UseIdentityColumn();
        builder.Property(item => item.JobDescriptionVersionId).HasColumnType("bigint").IsRequired();
        builder.Property(item => item.TaskCatalogItemId).HasColumnType("bigint").IsRequired();
        builder.Property(item => item.Title).HasMaxLength(300).IsRequired();
        builder.Property(item => item.Description).HasMaxLength(8000).IsRequired();
        builder.Property(item => item.StartDate).HasColumnType("date");
        builder.Property(item => item.EndDate).HasColumnType("date");
        builder.Property(item => item.WeeklyHours).HasPrecision(5, 2);
        builder.Property(item => item.SortOrder).IsRequired();
        builder.HasIndex(item => new { item.JobDescriptionVersionId, item.SortOrder }).IsUnique();
        builder.HasIndex(item => item.TaskCatalogItemId);
        builder.HasOne<TaskCatalogItem>()
            .WithMany()
            .HasForeignKey(item => item.TaskCatalogItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
