using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EosDashboards.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PreserveUnresolvedJobDescriptionInputs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE JobDescriptionVersions
                SET WorkflowStatus = N'PendingDataCompletion'
                WHERE WorkflowStatus = N'PendingDepartmentApproval'
                  AND (
                    PersonnelCode IS NULL OR LTRIM(RTRIM(PersonnelCode)) = N'' OR
                    Education = N'' OR FieldOfStudy = N'' OR MinimumExperience = N'' OR
                    NOT EXISTS (SELECT 1 FROM JobDescriptionVersionSkills s WHERE s.JobDescriptionVersionId = JobDescriptionVersions.Id) OR
                    NOT EXISTS (SELECT 1 FROM JobDescriptionTasks t WHERE t.JobDescriptionVersionId = JobDescriptionVersions.Id) OR
                    EXISTS (SELECT 1 FROM JobDescriptionTasks t WHERE t.JobDescriptionVersionId = JobDescriptionVersions.Id AND t.StartDate IS NULL)
                  );
                """);

            migrationBuilder.CreateTable(
                name: "JobDescriptionVersionUnresolvedSkills",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobDescriptionVersionId = table.Column<long>(type: "bigint", nullable: false),
                    RawName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobDescriptionVersionUnresolvedSkills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobDescriptionVersionUnresolvedSkills_JobDescriptionVersions_JobDescriptionVersionId",
                        column: x => x.JobDescriptionVersionId,
                        principalTable: "JobDescriptionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobDescriptionVersionUnresolvedTasks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobDescriptionVersionId = table.Column<long>(type: "bigint", nullable: false),
                    RawTitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobDescriptionVersionUnresolvedTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobDescriptionVersionUnresolvedTasks_JobDescriptionVersions_JobDescriptionVersionId",
                        column: x => x.JobDescriptionVersionId,
                        principalTable: "JobDescriptionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobDescriptionVersionUnresolvedSkills_JobDescriptionVersionId_SortOrder",
                table: "JobDescriptionVersionUnresolvedSkills",
                columns: new[] { "JobDescriptionVersionId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobDescriptionVersionUnresolvedTasks_JobDescriptionVersionId_SortOrder",
                table: "JobDescriptionVersionUnresolvedTasks",
                columns: new[] { "JobDescriptionVersionId", "SortOrder" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobDescriptionVersionUnresolvedSkills");

            migrationBuilder.DropTable(
                name: "JobDescriptionVersionUnresolvedTasks");
        }
    }
}
