using EosDashboards.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EosDashboards.Infrastructure.Persistence.Configurations;

internal sealed class JobDescriptionVersionSkillConfiguration : IEntityTypeConfiguration<JobDescriptionVersionSkill>
{
    public void Configure(EntityTypeBuilder<JobDescriptionVersionSkill> builder)
    {
        builder.ToTable("JobDescriptionVersionSkills");
        builder.HasKey(item => new { item.JobDescriptionVersionId, item.SkillCatalogItemId });
        builder.Property(item => item.JobDescriptionVersionId).HasColumnType("bigint");
        builder.Property(item => item.SkillCatalogItemId).HasColumnType("bigint");
        builder.HasOne(item => item.JobDescriptionVersion)
            .WithMany(item => item.Skills)
            .HasForeignKey(item => item.JobDescriptionVersionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.SkillCatalogItem)
            .WithMany()
            .HasForeignKey(item => item.SkillCatalogItemId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => item.SkillCatalogItemId);
    }
}
