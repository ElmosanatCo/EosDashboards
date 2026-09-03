using EosDashboards.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EosDashboards.Infrastructure.Persistence.Configurations;

internal sealed class ExternalIdentityLinkConfiguration : IEntityTypeConfiguration<ExternalIdentityLink>
{
    public void Configure(EntityTypeBuilder<ExternalIdentityLink> builder)
    {
        builder.ToTable("ExternalIdentityLinks");
        builder.HasKey(link => link.Id);
        builder.Property(link => link.Id).HasColumnType("bigint").UseIdentityColumn();
        builder.Property(link => link.Provider).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(link => link.NormalizedEmail).HasMaxLength(320).IsRequired();
        builder.Property(link => link.ProviderSubject).HasMaxLength(255);
        builder.Property(link => link.CreatedAtUtc).HasColumnType("datetimeoffset(7)");
        builder.Property(link => link.LinkedAtUtc).HasColumnType("datetimeoffset(7)");
        builder.Property<byte[]>("RowVersion").IsRequired().IsRowVersion();
        builder.HasIndex(link => new { link.Provider, link.NormalizedEmail }).IsUnique();
        builder.HasIndex(link => new { link.Provider, link.ProviderSubject })
            .IsUnique()
            .HasFilter("[ProviderSubject] IS NOT NULL");
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(link => link.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
