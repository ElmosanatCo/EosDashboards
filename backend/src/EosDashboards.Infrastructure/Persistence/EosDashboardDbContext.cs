using EosDashboards.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EosDashboards.Infrastructure.Persistence;

public sealed class EosDashboardDbContext(DbContextOptions<EosDashboardDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<OtpChallenge> OtpChallenges => Set<OtpChallenge>();

    public DbSet<UserSession> UserSessions => Set<UserSession>();

    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();

    public DbSet<ExternalIdentityLink> ExternalIdentityLinks => Set<ExternalIdentityLink>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<JobDescriptionVersion> JobDescriptionVersions => Set<JobDescriptionVersion>();

    public DbSet<JobDescriptionRecord> JobDescriptionRecords => Set<JobDescriptionRecord>();

    public DbSet<JobDescriptionTask> JobDescriptionTasks => Set<JobDescriptionTask>();

    public DbSet<JobDescriptionVersionSkill> JobDescriptionVersionSkills => Set<JobDescriptionVersionSkill>();

    public DbSet<JobDescriptionVersionUnresolvedSkill> JobDescriptionVersionUnresolvedSkills => Set<JobDescriptionVersionUnresolvedSkill>();

    public DbSet<JobDescriptionVersionUnresolvedTask> JobDescriptionVersionUnresolvedTasks => Set<JobDescriptionVersionUnresolvedTask>();

    public DbSet<SkillCatalogItem> SkillCatalogItems => Set<SkillCatalogItem>();

    public DbSet<TaskCatalogItem> TaskCatalogItems => Set<TaskCatalogItem>();

    public DbSet<TaskCatalogRequiredSkill> TaskCatalogRequiredSkills => Set<TaskCatalogRequiredSkill>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EosDashboardDbContext).Assembly);
    }
}
