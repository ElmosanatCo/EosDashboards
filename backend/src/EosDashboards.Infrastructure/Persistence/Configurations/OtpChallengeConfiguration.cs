using EosDashboards.Domain.Entities;
using EosDashboards.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EosDashboards.Infrastructure.Persistence.Configurations;

internal sealed class OtpChallengeConfiguration : IEntityTypeConfiguration<OtpChallenge>
{
    public void Configure(EntityTypeBuilder<OtpChallenge> builder)
    {
        builder.ToTable("OtpChallenges");
        builder.HasKey(challenge => challenge.Id);
        builder.Property(challenge => challenge.Id).HasColumnType("bigint").UseIdentityColumn();
        builder.Property(challenge => challenge.UserId).HasColumnType("bigint");
        builder.Property(challenge => challenge.PublicToken).HasMaxLength(128).IsRequired();
        builder.Property(challenge => challenge.CodeHash).HasMaxLength(512).IsRequired();
        builder.Property(challenge => challenge.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(challenge => challenge.Purpose)
            .HasConversion<string>()
            .HasMaxLength(32)
            .HasDefaultValue(OtpChallengePurpose.SignIn);
        builder.Property(challenge => challenge.CreatedAt).HasColumnType("datetime2(3)");
        builder.Property(challenge => challenge.ExpiresAt).HasColumnType("datetime2(3)");
        builder.Property(challenge => challenge.ResendAvailableAt).HasColumnType("datetime2(3)");
        builder.Property(challenge => challenge.ConsumedAt).HasColumnType("datetime2(3)");
        builder.Property<byte[]>("RowVersion").IsRequired().IsRowVersion().HasColumnType("rowversion");
        builder.HasIndex(challenge => challenge.PublicToken).IsUnique();
        builder.HasIndex(challenge => new { challenge.UserId, challenge.Status, challenge.CreatedAt });
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(challenge => challenge.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
