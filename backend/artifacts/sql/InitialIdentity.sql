IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902143905_InitialIdentity'
)
BEGIN
    CREATE TABLE [Roles] (
        [Id] bigint NOT NULL IDENTITY,
        [Code] nvarchar(100) NOT NULL,
        [DisplayName] nvarchar(200) NOT NULL,
        [IsActive] bit NOT NULL,
        [IsSystem] bit NOT NULL,
        [CreatedAtUtc] datetimeoffset(7) NOT NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902143905_InitialIdentity'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] bigint NOT NULL IDENTITY,
        [OrganizationalId] nvarchar(256) NOT NULL,
        [AccountName] nvarchar(256) NOT NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [ProtectedMobileNumber] nvarchar(2048) NOT NULL,
        [MaskedMobileNumber] nvarchar(64) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAtUtc] datetimeoffset(7) NOT NULL,
        [UpdatedAtUtc] datetimeoffset(7) NOT NULL,
        [DeactivatedAtUtc] datetimeoffset(7) NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902143905_InitialIdentity'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] bigint NOT NULL IDENTITY,
        [ActorUserId] bigint NULL,
        [SubjectUserId] bigint NULL,
        [EventCode] nvarchar(128) NOT NULL,
        [OccurredAtUtc] datetimeoffset(7) NOT NULL,
        [Succeeded] bit NOT NULL,
        [TraceId] nvarchar(128) NOT NULL,
        [SafeMetadata] nvarchar(4000) NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AuditLogs_Users_ActorUserId] FOREIGN KEY ([ActorUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AuditLogs_Users_SubjectUserId] FOREIGN KEY ([SubjectUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902143905_InitialIdentity'
)
BEGIN
    CREATE TABLE [OtpChallenges] (
        [Id] bigint NOT NULL IDENTITY,
        [UserId] bigint NOT NULL,
        [PublicToken] nvarchar(128) NOT NULL,
        [CodeHash] nvarchar(512) NOT NULL,
        [CreatedAtUtc] datetimeoffset(7) NOT NULL,
        [ExpiresAtUtc] datetimeoffset(7) NOT NULL,
        [ResendAvailableAtUtc] datetimeoffset(7) NOT NULL,
        [ConsumedAtUtc] datetimeoffset(7) NULL,
        [FailedAttemptCount] int NOT NULL,
        [Status] nvarchar(32) NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_OtpChallenges] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OtpChallenges_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902143905_InitialIdentity'
)
BEGIN
    CREATE TABLE [UserPreferences] (
        [Id] bigint NOT NULL IDENTITY,
        [UserId] bigint NOT NULL,
        [AppearanceMode] nvarchar(16) NOT NULL,
        [Palette] nvarchar(64) NOT NULL,
        [SidebarCollapsed] bit NOT NULL,
        [CreatedAtUtc] datetimeoffset(7) NOT NULL,
        [UpdatedAtUtc] datetimeoffset(7) NOT NULL,
        CONSTRAINT [PK_UserPreferences] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserPreferences_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902143905_InitialIdentity'
)
BEGIN
    CREATE TABLE [UserRoles] (
        [UserId] bigint NOT NULL,
        [RoleId] bigint NOT NULL,
        CONSTRAINT [PK_UserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902143905_InitialIdentity'
)
BEGIN
    CREATE TABLE [UserSessions] (
        [Id] bigint NOT NULL IDENTITY,
        [UserId] bigint NOT NULL,
        [RefreshCredentialHash] nvarchar(512) NOT NULL,
        [CreatedAtUtc] datetimeoffset(7) NOT NULL,
        [ExpiresAtUtc] datetimeoffset(7) NOT NULL,
        [LastRefreshedAtUtc] datetimeoffset(7) NULL,
        [RevokedAtUtc] datetimeoffset(7) NULL,
        [RevocationReason] nvarchar(32) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_UserSessions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserSessions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902143905_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_ActorUserId_OccurredAtUtc] ON [AuditLogs] ([ActorUserId], [OccurredAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902143905_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_EventCode_OccurredAtUtc] ON [AuditLogs] ([EventCode], [OccurredAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902143905_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_OccurredAtUtc] ON [AuditLogs] ([OccurredAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902143905_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_SubjectUserId_OccurredAtUtc] ON [AuditLogs] ([SubjectUserId], [OccurredAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902143905_InitialIdentity'
)
BEGIN
    CREATE UNIQUE INDEX [IX_OtpChallenges_PublicToken] ON [OtpChallenges] ([PublicToken]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902143905_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_OtpChallenges_UserId_Status_CreatedAtUtc] ON [OtpChallenges] ([UserId], [Status], [CreatedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902143905_InitialIdentity'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Roles_Code] ON [Roles] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902143905_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_Roles_IsActive] ON [Roles] ([IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902143905_InitialIdentity'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserPreferences_UserId] ON [UserPreferences] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902143905_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_UserRoles_RoleId] ON [UserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902143905_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_Users_AccountName] ON [Users] ([AccountName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902143905_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_Users_IsActive] ON [Users] ([IsActive]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902143905_InitialIdentity'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_OrganizationalId] ON [Users] ([OrganizationalId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902143905_InitialIdentity'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UserSessions_RefreshCredentialHash] ON [UserSessions] ([RefreshCredentialHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902143905_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_UserSessions_UserId_ExpiresAtUtc_RevokedAtUtc] ON [UserSessions] ([UserId], [ExpiresAtUtc], [RevokedAtUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260902143905_InitialIdentity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260902143905_InitialIdentity', N'10.0.11');
END;

COMMIT;
GO
