using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EosDashboards.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentsAndRoleDashboardFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "DepartmentId",
                table: "Users",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ParentDepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Departments_Departments_ParentDepartmentId",
                        column: x => x.ParentDepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql("""
                DECLARE @OccurredAtUtc datetimeoffset(7) = '2026-09-03T15:02:49+00:00';

                IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [Code] = N'SystemAdministrator')
                    INSERT INTO [Roles] ([Code], [DisplayName], [IsActive], [IsSystem], [CreatedAtUtc])
                    VALUES (N'SystemAdministrator', N'مدیر سامانه', 1, 1, @OccurredAtUtc);

                IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [Code] = N'DepartmentManager')
                    INSERT INTO [Roles] ([Code], [DisplayName], [IsActive], [IsSystem], [CreatedAtUtc])
                    VALUES (N'DepartmentManager', N'مدیر بخش', 1, 1, @OccurredAtUtc);

                IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [Code] = N'HumanResourcesManager')
                    INSERT INTO [Roles] ([Code], [DisplayName], [IsActive], [IsSystem], [CreatedAtUtc])
                    VALUES (N'HumanResourcesManager', N'مدیر منابع انسانی', 1, 1, @OccurredAtUtc);

                IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [Code] = N'ChiefExecutiveOfficer')
                    INSERT INTO [Roles] ([Code], [DisplayName], [IsActive], [IsSystem], [CreatedAtUtc])
                    VALUES (N'ChiefExecutiveOfficer', N'مدیرعامل', 1, 1, @OccurredAtUtc);

                IF NOT EXISTS (SELECT 1 FROM [Departments] WHERE [Name] = N'نرم افزار')
                    INSERT INTO [Departments] ([Name], [ParentDepartmentId], [CreatedAtUtc], [UpdatedAtUtc])
                    VALUES (N'نرم افزار', NULL, @OccurredAtUtc, @OccurredAtUtc);

                DECLARE @SoftwareDepartmentId bigint =
                    (SELECT [Id] FROM [Departments] WHERE [Name] = N'نرم افزار');

                IF EXISTS (SELECT 1 FROM [Departments] WHERE [Id] = @SoftwareDepartmentId AND [ParentDepartmentId] IS NOT NULL)
                    THROW 51000, 'The Software department must be an independent department.', 1;

                IF NOT EXISTS (SELECT 1 FROM [Departments] WHERE [Name] = N'فناوری اطلاعات')
                    INSERT INTO [Departments] ([Name], [ParentDepartmentId], [CreatedAtUtc], [UpdatedAtUtc])
                    VALUES (N'فناوری اطلاعات', @SoftwareDepartmentId, @OccurredAtUtc, @OccurredAtUtc);

                IF EXISTS (SELECT 1 FROM [Departments] WHERE [Name] = N'فناوری اطلاعات' AND ([ParentDepartmentId] IS NULL OR [ParentDepartmentId] <> @SoftwareDepartmentId))
                    THROW 51000, 'The Information Technology department must be a direct Software child.', 1;

                UPDATE [Users]
                SET [DepartmentId] = @SoftwareDepartmentId
                WHERE [Id] IN (
                    SELECT [UserId]
                    FROM [UserRoles]
                    INNER JOIN [Roles] ON [Roles].[Id] = [UserRoles].[RoleId]
                    WHERE [Roles].[Code] = N'SystemAdministrator')
                    AND [DepartmentId] IS NULL;

                DECLARE @DepartmentManagerRoleId bigint =
                    (SELECT [Id] FROM [Roles] WHERE [Code] = N'DepartmentManager');

                INSERT INTO [UserRoles] ([UserId], [RoleId])
                SELECT [Users].[Id], @DepartmentManagerRoleId
                FROM [Users]
                INNER JOIN [UserRoles] AS [SystemAdministratorAssignment]
                    ON [SystemAdministratorAssignment].[UserId] = [Users].[Id]
                INNER JOIN [Roles] AS [SystemAdministratorRole]
                    ON [SystemAdministratorRole].[Id] = [SystemAdministratorAssignment].[RoleId]
                WHERE [SystemAdministratorRole].[Code] = N'SystemAdministrator'
                    AND NOT EXISTS (
                        SELECT 1 FROM [UserRoles]
                        WHERE [UserRoles].[UserId] = [Users].[Id]
                            AND [UserRoles].[RoleId] = @DepartmentManagerRoleId);

                IF EXISTS (SELECT 1 FROM [Users] WHERE [DepartmentId] IS NULL)
                    THROW 51000, 'Every existing user must already be a System Administrator before this migration.', 1;
                """);

            migrationBuilder.AlterColumn<long>(
                name: "DepartmentId",
                table: "Users",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_DepartmentId",
                table: "Users",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_ParentDepartmentId",
                table: "Departments",
                column: "ParentDepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Departments_DepartmentId",
                table: "Users",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Departments_DepartmentId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Users_DepartmentId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Users");
        }
    }
}
