using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EosDashboards.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DepartmentJobDescriptionFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JobDescriptionRecords",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: false),
                    PersonName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobDescriptionRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobDescriptionRecords_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SkillCatalogItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillCatalogItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkillCatalogItems_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaskCatalogItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    IsProject = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskCatalogItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskCatalogItems_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JobDescriptionVersions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobDescriptionRecordId = table.Column<long>(type: "bigint", nullable: true),
                    PersonName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: false),
                    PersonnelCode = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Education = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    FieldOfStudy = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    MinimumExperience = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    WorkflowStatus = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    DepartmentApprovedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    HumanResourcesReviewedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ExcelArtifact = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    ExcelFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobDescriptionVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobDescriptionVersions_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobDescriptionVersions_JobDescriptionRecords_JobDescriptionRecordId",
                        column: x => x.JobDescriptionRecordId,
                        principalTable: "JobDescriptionRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaskCatalogRequiredSkills",
                columns: table => new
                {
                    TaskCatalogItemId = table.Column<long>(type: "bigint", nullable: false),
                    SkillCatalogItemId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskCatalogRequiredSkills", x => new { x.TaskCatalogItemId, x.SkillCatalogItemId });
                    table.ForeignKey(
                        name: "FK_TaskCatalogRequiredSkills_SkillCatalogItems_SkillCatalogItemId",
                        column: x => x.SkillCatalogItemId,
                        principalTable: "SkillCatalogItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaskCatalogRequiredSkills_TaskCatalogItems_TaskCatalogItemId",
                        column: x => x.TaskCatalogItemId,
                        principalTable: "TaskCatalogItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JobDescriptionTasks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobDescriptionVersionId = table.Column<long>(type: "bigint", nullable: false),
                    TaskCatalogItemId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobDescriptionTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobDescriptionTasks_JobDescriptionVersions_JobDescriptionVersionId",
                        column: x => x.JobDescriptionVersionId,
                        principalTable: "JobDescriptionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobDescriptionTasks_TaskCatalogItems_TaskCatalogItemId",
                        column: x => x.TaskCatalogItemId,
                        principalTable: "TaskCatalogItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JobDescriptionVersionSkills",
                columns: table => new
                {
                    JobDescriptionVersionId = table.Column<long>(type: "bigint", nullable: false),
                    SkillCatalogItemId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobDescriptionVersionSkills", x => new { x.JobDescriptionVersionId, x.SkillCatalogItemId });
                    table.ForeignKey(
                        name: "FK_JobDescriptionVersionSkills_JobDescriptionVersions_JobDescriptionVersionId",
                        column: x => x.JobDescriptionVersionId,
                        principalTable: "JobDescriptionVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobDescriptionVersionSkills_SkillCatalogItems_SkillCatalogItemId",
                        column: x => x.SkillCatalogItemId,
                        principalTable: "SkillCatalogItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobDescriptionRecords_DepartmentId_PersonName",
                table: "JobDescriptionRecords",
                columns: new[] { "DepartmentId", "PersonName" });

            migrationBuilder.CreateIndex(
                name: "IX_JobDescriptionTasks_JobDescriptionVersionId_SortOrder",
                table: "JobDescriptionTasks",
                columns: new[] { "JobDescriptionVersionId", "SortOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobDescriptionTasks_TaskCatalogItemId",
                table: "JobDescriptionTasks",
                column: "TaskCatalogItemId");

            migrationBuilder.CreateIndex(
                name: "IX_JobDescriptionVersions_DepartmentId_PersonName",
                table: "JobDescriptionVersions",
                columns: new[] { "DepartmentId", "PersonName" });

            migrationBuilder.CreateIndex(
                name: "IX_JobDescriptionVersions_JobDescriptionRecordId",
                table: "JobDescriptionVersions",
                column: "JobDescriptionRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_JobDescriptionVersions_WorkflowStatus",
                table: "JobDescriptionVersions",
                column: "WorkflowStatus");

            migrationBuilder.CreateIndex(
                name: "IX_JobDescriptionVersionSkills_SkillCatalogItemId",
                table: "JobDescriptionVersionSkills",
                column: "SkillCatalogItemId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillCatalogItems_DepartmentId",
                table: "SkillCatalogItems",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillCatalogItems_DepartmentId_Name",
                table: "SkillCatalogItems",
                columns: new[] { "DepartmentId", "Name" },
                unique: true,
                filter: "[DepartmentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SkillCatalogItems_Name",
                table: "SkillCatalogItems",
                column: "Name",
                unique: true,
                filter: "[DepartmentId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TaskCatalogItems_DepartmentId",
                table: "TaskCatalogItems",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskCatalogItems_DepartmentId_Title",
                table: "TaskCatalogItems",
                columns: new[] { "DepartmentId", "Title" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskCatalogRequiredSkills_SkillCatalogItemId",
                table: "TaskCatalogRequiredSkills",
                column: "SkillCatalogItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobDescriptionTasks");

            migrationBuilder.DropTable(
                name: "JobDescriptionVersionSkills");

            migrationBuilder.DropTable(
                name: "TaskCatalogRequiredSkills");

            migrationBuilder.DropTable(
                name: "JobDescriptionVersions");

            migrationBuilder.DropTable(
                name: "SkillCatalogItems");

            migrationBuilder.DropTable(
                name: "TaskCatalogItems");

            migrationBuilder.DropTable(
                name: "JobDescriptionRecords");
        }
    }
}
