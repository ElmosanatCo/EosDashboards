using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EosDashboards.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PublicSkillOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "OwnerDepartmentId",
                table: "SkillCatalogItems",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SkillCatalogItems_OwnerDepartmentId",
                table: "SkillCatalogItems",
                column: "OwnerDepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_SkillCatalogItems_Departments_OwnerDepartmentId",
                table: "SkillCatalogItems",
                column: "OwnerDepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SkillCatalogItems_Departments_OwnerDepartmentId",
                table: "SkillCatalogItems");

            migrationBuilder.DropIndex(
                name: "IX_SkillCatalogItems_OwnerDepartmentId",
                table: "SkillCatalogItems");

            migrationBuilder.DropColumn(
                name: "OwnerDepartmentId",
                table: "SkillCatalogItems");
        }
    }
}
