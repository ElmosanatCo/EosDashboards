using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EosDashboards.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditRequestAttribution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientDeviceKind",
                table: "AuditLogs",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientIpAddress",
                table: "AuditLogs",
                type: "nvarchar(45)",
                maxLength: 45,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientDeviceKind",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "ClientIpAddress",
                table: "AuditLogs");
        }
    }
}
