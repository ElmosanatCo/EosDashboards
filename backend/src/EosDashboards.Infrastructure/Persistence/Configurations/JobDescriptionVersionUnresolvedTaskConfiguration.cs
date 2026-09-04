using EosDashboards.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EosDashboards.Infrastructure.Persistence.Configurations;

internal sealed class JobDescriptionVersionUnresolvedTaskConfiguration : IEntityTypeConfiguration<JobDescriptionVersionUnresolvedTask>
{
    public void Configure(EntityTypeBuilder<JobDescriptionVersionUnresolvedTask> builder)
    {
        builder.ToTable("JobDescriptionVersionUnresolvedTasks");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasColumnType("bigint").UseIdentityColumn();
        builder.Property(item => item.JobDescriptionVersionId).HasColumnType("bigint").IsRequired();
        builder.Property(item => item.RawTitle).HasMaxLength(300).IsRequired();
        builder.Property(item => item.Description).HasMaxLength(8000).IsRequired();
        builder.Property(item => item.StartDate).HasColumnType("date");
        builder.Property(item => item.EndDate).HasColumnType("date");
        builder.Property(item => item.SortOrder).IsRequired();
        builder.HasIndex(item => new { item.JobDescriptionVersionId, item.SortOrder }).IsUnique();
    }
}
