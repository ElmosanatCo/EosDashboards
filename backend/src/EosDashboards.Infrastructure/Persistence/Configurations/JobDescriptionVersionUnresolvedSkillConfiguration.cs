using EosDashboards.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EosDashboards.Infrastructure.Persistence.Configurations;

internal sealed class JobDescriptionVersionUnresolvedSkillConfiguration : IEntityTypeConfiguration<JobDescriptionVersionUnresolvedSkill>
{
    public void Configure(EntityTypeBuilder<JobDescriptionVersionUnresolvedSkill> builder)
    {
        builder.ToTable("JobDescriptionVersionUnresolvedSkills");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasColumnType("bigint").UseIdentityColumn();
        builder.Property(item => item.JobDescriptionVersionId).HasColumnType("bigint").IsRequired();
        builder.Property(item => item.RawName).HasMaxLength(500).IsRequired();
        builder.Property(item => item.SortOrder).IsRequired();
        builder.HasIndex(item => new { item.JobDescriptionVersionId, item.SortOrder }).IsUnique();
    }
}
