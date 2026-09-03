using EosDashboards.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EosDashboards.Infrastructure.Persistence.Configurations;

internal sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("UserSessions");
        builder.HasKey(session => session.Id);
        builder.Property(session => session.Id).HasColumnType("bigint").UseIdentityColumn();
        builder.Property(session => session.UserId).HasColumnType("bigint");
        builder.Property(session => session.RefreshCredentialHash).HasMaxLength(512).IsRequired();
        builder.Property(session => session.CreatedAtUtc).HasColumnType("datetimeoffset(7)");
        builder.Property(session => session.ExpiresAtUtc).HasColumnType("datetimeoffset(7)");
        builder.Property(session => session.LastRefreshedAtUtc).HasColumnType("datetimeoffset(7)");
        builder.Property(session => session.RevokedAtUtc).HasColumnType("datetimeoffset(7)");
        builder.Property(session => session.RevocationReason).HasConversion<string>().HasMaxLength(32);
        builder.Property<byte[]>("RowVersion").IsRequired().IsRowVersion().HasColumnType("rowversion");
        builder.HasIndex(session => session.RefreshCredentialHash).IsUnique();
        builder.HasIndex(session => new { session.UserId, session.ExpiresAtUtc, session.RevokedAtUtc });
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(session => session.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
