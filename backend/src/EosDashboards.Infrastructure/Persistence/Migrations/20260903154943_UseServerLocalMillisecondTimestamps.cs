using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EosDashboards.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UseServerLocalMillisecondTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            DropTimestampIndexes(migrationBuilder, true);

            ConvertAndRename(migrationBuilder, "AuditLogs", "OccurredAtUtc", "OccurredAt", false);
            ConvertAndRename(migrationBuilder, "Departments", "CreatedAtUtc", "CreatedAt", false);
            ConvertAndRename(migrationBuilder, "Departments", "UpdatedAtUtc", "UpdatedAt", false);
            ConvertAndRename(migrationBuilder, "ExternalIdentityLinks", "CreatedAtUtc", "CreatedAt", false);
            ConvertAndRename(migrationBuilder, "ExternalIdentityLinks", "LinkedAtUtc", "LinkedAt", true);
            ConvertAndRename(migrationBuilder, "OtpChallenges", "ConsumedAtUtc", "ConsumedAt", true);
            ConvertAndRename(migrationBuilder, "OtpChallenges", "CreatedAtUtc", "CreatedAt", false);
            ConvertAndRename(migrationBuilder, "OtpChallenges", "ExpiresAtUtc", "ExpiresAt", false);
            ConvertAndRename(migrationBuilder, "OtpChallenges", "ResendAvailableAtUtc", "ResendAvailableAt", false);
            ConvertAndRename(migrationBuilder, "Roles", "CreatedAtUtc", "CreatedAt", false);
            ConvertAndRename(migrationBuilder, "UserPreferences", "CreatedAtUtc", "CreatedAt", false);
            ConvertAndRename(migrationBuilder, "UserPreferences", "UpdatedAtUtc", "UpdatedAt", false);
            ConvertAndRename(migrationBuilder, "UserSessions", "CreatedAtUtc", "CreatedAt", false);
            ConvertAndRename(migrationBuilder, "UserSessions", "ExpiresAtUtc", "ExpiresAt", false);
            ConvertAndRename(migrationBuilder, "UserSessions", "LastRefreshedAtUtc", "LastRefreshedAt", true);
            ConvertAndRename(migrationBuilder, "UserSessions", "RevokedAtUtc", "RevokedAt", true);
            ConvertAndRename(migrationBuilder, "Users", "CreatedAtUtc", "CreatedAt", false);
            ConvertAndRename(migrationBuilder, "Users", "DeactivatedAtUtc", "DeactivatedAt", true);
            ConvertAndRename(migrationBuilder, "Users", "UpdatedAtUtc", "UpdatedAt", false);

            CreateTimestampIndexes(migrationBuilder, false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            DropTimestampIndexes(migrationBuilder, false);

            RestoreLegacyColumn(migrationBuilder, "AuditLogs", "OccurredAt", "OccurredAtUtc", false);
            RestoreLegacyColumn(migrationBuilder, "Departments", "CreatedAt", "CreatedAtUtc", false);
            RestoreLegacyColumn(migrationBuilder, "Departments", "UpdatedAt", "UpdatedAtUtc", false);
            RestoreLegacyColumn(migrationBuilder, "ExternalIdentityLinks", "CreatedAt", "CreatedAtUtc", false);
            RestoreLegacyColumn(migrationBuilder, "ExternalIdentityLinks", "LinkedAt", "LinkedAtUtc", true);
            RestoreLegacyColumn(migrationBuilder, "OtpChallenges", "ConsumedAt", "ConsumedAtUtc", true);
            RestoreLegacyColumn(migrationBuilder, "OtpChallenges", "CreatedAt", "CreatedAtUtc", false);
            RestoreLegacyColumn(migrationBuilder, "OtpChallenges", "ExpiresAt", "ExpiresAtUtc", false);
            RestoreLegacyColumn(migrationBuilder, "OtpChallenges", "ResendAvailableAt", "ResendAvailableAtUtc", false);
            RestoreLegacyColumn(migrationBuilder, "Roles", "CreatedAt", "CreatedAtUtc", false);
            RestoreLegacyColumn(migrationBuilder, "UserPreferences", "CreatedAt", "CreatedAtUtc", false);
            RestoreLegacyColumn(migrationBuilder, "UserPreferences", "UpdatedAt", "UpdatedAtUtc", false);
            RestoreLegacyColumn(migrationBuilder, "UserSessions", "CreatedAt", "CreatedAtUtc", false);
            RestoreLegacyColumn(migrationBuilder, "UserSessions", "ExpiresAt", "ExpiresAtUtc", false);
            RestoreLegacyColumn(migrationBuilder, "UserSessions", "LastRefreshedAt", "LastRefreshedAtUtc", true);
            RestoreLegacyColumn(migrationBuilder, "UserSessions", "RevokedAt", "RevokedAtUtc", true);
            RestoreLegacyColumn(migrationBuilder, "Users", "CreatedAt", "CreatedAtUtc", false);
            RestoreLegacyColumn(migrationBuilder, "Users", "DeactivatedAt", "DeactivatedAtUtc", true);
            RestoreLegacyColumn(migrationBuilder, "Users", "UpdatedAt", "UpdatedAtUtc", false);

            CreateTimestampIndexes(migrationBuilder, true);
        }

        private static void ConvertAndRename(MigrationBuilder migrationBuilder, string table, string oldName, string newName, bool nullable)
        {
            migrationBuilder.Sql($"UPDATE [{table}] SET [{oldName}] = SWITCHOFFSET([{oldName}], DATENAME(TzOffset, SYSDATETIMEOFFSET())) WHERE [{oldName}] IS NOT NULL;");
            migrationBuilder.RenameColumn(name: oldName, table: table, newName: newName);
            migrationBuilder.AlterColumn<DateTime>(name: newName, table: table, type: "datetime2(3)", nullable: nullable, oldClrType: typeof(DateTimeOffset), oldType: "datetimeoffset(7)", oldNullable: nullable);
        }

        private static void RestoreLegacyColumn(MigrationBuilder migrationBuilder, string table, string newName, string oldName, bool nullable)
        {
            migrationBuilder.AlterColumn<DateTimeOffset>(name: newName, table: table, type: "datetimeoffset(7)", nullable: nullable, oldClrType: typeof(DateTime), oldType: "datetime2(3)", oldNullable: nullable);
            migrationBuilder.RenameColumn(name: newName, table: table, newName: oldName);
        }

        private static void DropTimestampIndexes(MigrationBuilder migrationBuilder, bool oldNames)
        {
            var audit = oldNames ? "OccurredAtUtc" : "OccurredAt";
            var otp = oldNames ? "CreatedAtUtc" : "CreatedAt";
            var expires = oldNames ? "ExpiresAtUtc" : "ExpiresAt";
            var revoked = oldNames ? "RevokedAtUtc" : "RevokedAt";
            migrationBuilder.DropIndex($"IX_UserSessions_UserId_{expires}_{revoked}", "UserSessions");
            migrationBuilder.DropIndex($"IX_OtpChallenges_UserId_Status_{otp}", "OtpChallenges");
            migrationBuilder.DropIndex($"IX_AuditLogs_ActorUserId_{audit}", "AuditLogs");
            migrationBuilder.DropIndex($"IX_AuditLogs_EventCode_{audit}", "AuditLogs");
            migrationBuilder.DropIndex($"IX_AuditLogs_{audit}", "AuditLogs");
            migrationBuilder.DropIndex($"IX_AuditLogs_SubjectUserId_{audit}", "AuditLogs");
        }

        private static void CreateTimestampIndexes(MigrationBuilder migrationBuilder, bool oldNames)
        {
            var audit = oldNames ? "OccurredAtUtc" : "OccurredAt";
            var otp = oldNames ? "CreatedAtUtc" : "CreatedAt";
            var expires = oldNames ? "ExpiresAtUtc" : "ExpiresAt";
            var revoked = oldNames ? "RevokedAtUtc" : "RevokedAt";
            migrationBuilder.CreateIndex($"IX_UserSessions_UserId_{expires}_{revoked}", "UserSessions", new[] { "UserId", expires, revoked });
            migrationBuilder.CreateIndex($"IX_OtpChallenges_UserId_Status_{otp}", "OtpChallenges", new[] { "UserId", "Status", otp });
            migrationBuilder.CreateIndex($"IX_AuditLogs_ActorUserId_{audit}", "AuditLogs", new[] { "ActorUserId", audit });
            migrationBuilder.CreateIndex($"IX_AuditLogs_EventCode_{audit}", "AuditLogs", new[] { "EventCode", audit });
            migrationBuilder.CreateIndex($"IX_AuditLogs_{audit}", "AuditLogs", audit);
            migrationBuilder.CreateIndex($"IX_AuditLogs_SubjectUserId_{audit}", "AuditLogs", new[] { "SubjectUserId", audit });
        }
    }
}
