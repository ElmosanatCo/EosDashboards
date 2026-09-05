using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EosDashboards.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddJobDescriptionReviewWarning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NeedsReview",
                table: "JobDescriptionVersions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                DECLARE @Now datetime2(3) = SYSDATETIME();

                ;WITH Quality AS
                (
                    SELECT
                        version.Id,
                        HasMissingRequiredSkill = CONVERT(bit, CASE WHEN EXISTS
                        (
                            SELECT 1
                            FROM JobDescriptionTasks AS jobTask
                            INNER JOIN TaskCatalogRequiredSkills AS requiredSkill
                                ON requiredSkill.TaskCatalogItemId = jobTask.TaskCatalogItemId
                            WHERE jobTask.JobDescriptionVersionId = version.Id
                              AND NOT EXISTS
                              (
                                  SELECT 1
                                  FROM JobDescriptionVersionSkills AS selectedSkill
                                  WHERE selectedSkill.JobDescriptionVersionId = version.Id
                                    AND selectedSkill.SkillCatalogItemId = requiredSkill.SkillCatalogItemId
                              )
                        ) THEN 1 ELSE 0 END),
                        HasBlockingIssue = CONVERT(bit, CASE WHEN
                            EXISTS
                            (
                                SELECT 1
                                FROM JobDescriptionVersionUnresolvedSkills AS unresolvedSkill
                                WHERE unresolvedSkill.JobDescriptionVersionId = version.Id
                            )
                            OR EXISTS
                            (
                                SELECT 1
                                FROM JobDescriptionVersionUnresolvedTasks AS unresolvedTask
                                WHERE unresolvedTask.JobDescriptionVersionId = version.Id
                            )
                            OR EXISTS
                            (
                                SELECT 1
                                FROM JobDescriptionTasks AS jobTask
                                WHERE jobTask.JobDescriptionVersionId = version.Id
                                  AND (jobTask.StartDate IS NULL OR jobTask.WeeklyHours IS NULL)
                            )
                            OR EXISTS
                            (
                                SELECT 1
                                FROM JobDescriptionTasks AS jobTask
                                LEFT JOIN TaskCatalogItems AS catalogTask
                                    ON catalogTask.Id = jobTask.TaskCatalogItemId
                                WHERE jobTask.JobDescriptionVersionId = version.Id
                                  AND catalogTask.Id IS NULL
                            )
                            OR NULLIF(LTRIM(RTRIM(version.PersonName)), N'') IS NULL
                            OR NULLIF(LTRIM(RTRIM(version.PersonnelCode)), N'') IS NULL
                            OR NULLIF(LTRIM(RTRIM(version.Education)), N'') IS NULL
                            OR NULLIF(LTRIM(RTRIM(version.FieldOfStudy)), N'') IS NULL
                            OR NULLIF(LTRIM(RTRIM(version.MinimumExperience)), N'') IS NULL
                            OR NOT EXISTS
                            (
                                SELECT 1
                                FROM JobDescriptionVersionSkills AS selectedSkill
                                WHERE selectedSkill.JobDescriptionVersionId = version.Id
                            )
                            OR NOT EXISTS
                            (
                                SELECT 1
                                FROM JobDescriptionTasks AS jobTask
                                WHERE jobTask.JobDescriptionVersionId = version.Id
                            )
                        THEN 1 ELSE 0 END),
                        IsComplete = CONVERT(bit, CASE WHEN
                            NULLIF(LTRIM(RTRIM(version.PersonName)), N'') IS NOT NULL
                            AND NULLIF(LTRIM(RTRIM(version.PersonnelCode)), N'') IS NOT NULL
                            AND NULLIF(LTRIM(RTRIM(version.Education)), N'') IS NOT NULL
                            AND NULLIF(LTRIM(RTRIM(version.FieldOfStudy)), N'') IS NOT NULL
                            AND NULLIF(LTRIM(RTRIM(version.MinimumExperience)), N'') IS NOT NULL
                            AND EXISTS
                            (
                                SELECT 1
                                FROM JobDescriptionVersionSkills AS selectedSkill
                                WHERE selectedSkill.JobDescriptionVersionId = version.Id
                            )
                            AND EXISTS
                            (
                                SELECT 1
                                FROM JobDescriptionTasks AS jobTask
                                WHERE jobTask.JobDescriptionVersionId = version.Id
                            )
                            AND NOT EXISTS
                            (
                                SELECT 1
                                FROM JobDescriptionVersionUnresolvedSkills AS unresolvedSkill
                                WHERE unresolvedSkill.JobDescriptionVersionId = version.Id
                            )
                            AND NOT EXISTS
                            (
                                SELECT 1
                                FROM JobDescriptionVersionUnresolvedTasks AS unresolvedTask
                                WHERE unresolvedTask.JobDescriptionVersionId = version.Id
                            )
                            AND NOT EXISTS
                            (
                                SELECT 1
                                FROM JobDescriptionTasks AS jobTask
                                WHERE jobTask.JobDescriptionVersionId = version.Id
                                  AND (jobTask.StartDate IS NULL OR jobTask.WeeklyHours IS NULL)
                            )
                            AND NOT EXISTS
                            (
                                SELECT 1
                                FROM JobDescriptionTasks AS jobTask
                                LEFT JOIN TaskCatalogItems AS catalogTask
                                    ON catalogTask.Id = jobTask.TaskCatalogItemId
                                WHERE jobTask.JobDescriptionVersionId = version.Id
                                  AND catalogTask.Id IS NULL
                            )
                        THEN 1 ELSE 0 END)
                    FROM JobDescriptionVersions AS version
                    WHERE version.WorkflowStatus NOT IN (N'Approved', N'Archived')
                )
                UPDATE version
                SET NeedsReview = quality.HasMissingRequiredSkill,
                    HasCatalogQualityIssues = quality.HasBlockingIssue,
                    WorkflowStatus = CASE
                        WHEN quality.HasBlockingIssue = 1 THEN N'PendingDataCompletion'
                        WHEN version.WorkflowStatus = N'PendingDataCompletion' AND quality.IsComplete = 1
                            THEN N'PendingDepartmentApproval'
                        ELSE version.WorkflowStatus
                    END,
                    DepartmentApprovedAt = CASE
                        WHEN quality.HasBlockingIssue = 1
                          OR (version.WorkflowStatus = N'PendingDataCompletion' AND quality.IsComplete = 1)
                            THEN NULL
                        ELSE version.DepartmentApprovedAt
                    END,
                    HumanResourcesReviewedAt = CASE
                        WHEN quality.HasBlockingIssue = 1
                          OR (version.WorkflowStatus = N'PendingDataCompletion' AND quality.IsComplete = 1)
                            THEN NULL
                        ELSE version.HumanResourcesReviewedAt
                    END,
                    RejectionReason = CASE
                        WHEN quality.HasBlockingIssue = 1
                          OR (version.WorkflowStatus = N'PendingDataCompletion' AND quality.IsComplete = 1)
                            THEN NULL
                        ELSE version.RejectionReason
                    END,
                    UpdatedAt = @Now
                FROM JobDescriptionVersions AS version
                INNER JOIN Quality AS quality ON quality.Id = version.Id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NeedsReview",
                table: "JobDescriptionVersions");
        }
    }
}
