using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EosDashboards.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExternalIdentityLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExternalIdentityLinks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    ProviderSubject = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    LinkedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalIdentityLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalIdentityLinks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalIdentityLinks_Provider_NormalizedEmail",
                table: "ExternalIdentityLinks",
                columns: new[] { "Provider", "NormalizedEmail" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExternalIdentityLinks_Provider_ProviderSubject",
                table: "ExternalIdentityLinks",
                columns: new[] { "Provider", "ProviderSubject" },
                unique: true,
                filter: "[ProviderSubject] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalIdentityLinks_UserId",
                table: "ExternalIdentityLinks",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExternalIdentityLinks");
        }
    }
}
