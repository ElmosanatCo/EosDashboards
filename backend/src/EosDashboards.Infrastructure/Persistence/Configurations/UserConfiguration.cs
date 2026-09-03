using EosDashboards.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EosDashboards.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).HasColumnType("bigint").UseIdentityColumn();
        builder.Property(user => user.OrganizationalId).HasMaxLength(256).IsRequired();
        builder.Property(user => user.AccountName).HasMaxLength(256).IsRequired();
        builder.Property(user => user.Username).HasMaxLength(256);
        builder.Property(user => user.PasswordHash).HasMaxLength(1024);
        builder.Property(user => user.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(user => user.LastName).HasMaxLength(100).IsRequired();
        builder.Property(user => user.ProtectedMobileNumber).HasMaxLength(2048).IsRequired();
        builder.Property(user => user.MaskedMobileNumber).HasMaxLength(64).IsRequired();
        builder.Property(user => user.DepartmentId).HasColumnType("bigint").IsRequired();
        builder.Property(user => user.CreatedAtUtc).HasColumnType("datetimeoffset(7)");
        builder.Property(user => user.UpdatedAtUtc).HasColumnType("datetimeoffset(7)");
        builder.Property(user => user.DeactivatedAtUtc).HasColumnType("datetimeoffset(7)");
        builder.HasIndex(user => user.OrganizationalId).IsUnique();
        builder.HasIndex(user => user.AccountName);
        builder.HasIndex(user => user.Username).IsUnique().HasFilter("[Username] IS NOT NULL");
        builder.HasIndex(user => user.IsActive);
        builder.HasIndex(user => user.DepartmentId);
        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(user => user.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(user => user.UserRoles).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
