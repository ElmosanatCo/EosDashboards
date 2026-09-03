using EosDashboards.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EosDashboards.Infrastructure.Persistence.Configurations;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(audit => audit.Id);
        builder.Property(audit => audit.Id).HasColumnType("bigint").UseIdentityColumn();
        builder.Property(audit => audit.ActorUserId).HasColumnType("bigint");
        builder.Property(audit => audit.SubjectUserId).HasColumnType("bigint");
        builder.Property(audit => audit.EventCode).HasMaxLength(128).IsRequired();
        builder.Property(audit => audit.OccurredAt).HasColumnType("datetime2(3)");
        builder.Property(audit => audit.TraceId).HasMaxLength(128).IsRequired();
        builder.Property(audit => audit.SafeMetadata).HasMaxLength(4000);
        builder.HasIndex(audit => audit.OccurredAt);
        builder.HasIndex(audit => new { audit.ActorUserId, audit.OccurredAt });
        builder.HasIndex(audit => new { audit.SubjectUserId, audit.OccurredAt });
        builder.HasIndex(audit => new { audit.EventCode, audit.OccurredAt });
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(audit => audit.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(audit => audit.SubjectUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
