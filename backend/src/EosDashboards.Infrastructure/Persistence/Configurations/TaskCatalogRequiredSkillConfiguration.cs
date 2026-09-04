using EosDashboards.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EosDashboards.Infrastructure.Persistence.Configurations;

internal sealed class TaskCatalogRequiredSkillConfiguration : IEntityTypeConfiguration<TaskCatalogRequiredSkill>
{
    public void Configure(EntityTypeBuilder<TaskCatalogRequiredSkill> builder)
    {
        builder.ToTable("TaskCatalogRequiredSkills");
        builder.HasKey(item => new { item.TaskCatalogItemId, item.SkillCatalogItemId });
        builder.Property(item => item.TaskCatalogItemId).HasColumnType("bigint");
        builder.Property(item => item.SkillCatalogItemId).HasColumnType("bigint");
        builder.HasOne(item => item.TaskCatalogItem)
            .WithMany(item => item.RequiredSkills)
            .HasForeignKey(item => item.TaskCatalogItemId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.SkillCatalogItem)
            .WithMany()
            .HasForeignKey(item => item.SkillCatalogItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
