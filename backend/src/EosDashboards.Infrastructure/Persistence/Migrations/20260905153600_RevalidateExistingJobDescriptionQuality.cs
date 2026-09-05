using EosDashboards.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EosDashboards.Infrastructure.Persistence.Migrations;

[DbContext(typeof(EosDashboardDbContext))]
[Migration("20260905153600_RevalidateExistingJobDescriptionQuality")]
public partial class RevalidateExistingJobDescriptionQuality : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE version
            SET HasCatalogQualityIssues = 1,
                WorkflowStatus = N'PendingDataCompletion',
                DepartmentApprovedAt = NULL,
                HumanResourcesReviewedAt = NULL,
                RejectionReason = NULL,
                UpdatedAt = SYSDATETIME()
            FROM JobDescriptionVersions AS version
            WHERE version.WorkflowStatus NOT IN (N'Approved', N'Archived')
              AND (
                  EXISTS (
                      SELECT 1
                      FROM JobDescriptionVersionUnresolvedSkills AS unresolvedSkill
                      WHERE unresolvedSkill.JobDescriptionVersionId = version.Id
                  )
                  OR EXISTS (
                      SELECT 1
                      FROM JobDescriptionVersionUnresolvedTasks AS unresolvedTask
                      WHERE unresolvedTask.JobDescriptionVersionId = version.Id
                  )
                  OR EXISTS (
                      SELECT 1
                      FROM JobDescriptionTasks AS jobTask
                      WHERE jobTask.JobDescriptionVersionId = version.Id
                        AND (jobTask.StartDate IS NULL OR jobTask.WeeklyHours IS NULL)
                  )
                  OR EXISTS (
                      SELECT 1
                      FROM JobDescriptionTasks AS jobTask
                      INNER JOIN TaskCatalogRequiredSkills AS requiredSkill
                          ON requiredSkill.TaskCatalogItemId = jobTask.TaskCatalogItemId
                      WHERE jobTask.JobDescriptionVersionId = version.Id
                        AND NOT EXISTS (
                            SELECT 1
                            FROM JobDescriptionVersionSkills AS selectedSkill
                            WHERE selectedSkill.JobDescriptionVersionId = version.Id
                              AND selectedSkill.SkillCatalogItemId = requiredSkill.SkillCatalogItemId
                        )
                  )
                  OR EXISTS (
                      SELECT 1
                      FROM JobDescriptionVersionSkills AS selectedSkill
                      WHERE selectedSkill.JobDescriptionVersionId = version.Id
                        AND NOT EXISTS (
                            SELECT 1
                            FROM JobDescriptionTasks AS jobTask
                            INNER JOIN TaskCatalogRequiredSkills AS requiredSkill
                                ON requiredSkill.TaskCatalogItemId = jobTask.TaskCatalogItemId
                            WHERE jobTask.JobDescriptionVersionId = version.Id
                              AND requiredSkill.SkillCatalogItemId = selectedSkill.SkillCatalogItemId
                        )
                  )
              );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
