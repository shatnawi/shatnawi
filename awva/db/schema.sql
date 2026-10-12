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
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012103053_InitialCreate'
)
BEGIN
    CREATE TABLE [Targets] (
        [Id] int NOT NULL IDENTITY,
        [Url] nvarchar(2048) NOT NULL,
        [Host] nvarchar(255) NOT NULL,
        [ResolvedIp] nvarchar(64) NULL,
        [Profile] int NOT NULL,
        [IsApproved] bit NOT NULL,
        [RejectionReason] nvarchar(max) NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_Targets] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012103053_InitialCreate'
)
BEGIN
    CREATE TABLE [GroundTruthItems] (
        [Id] int NOT NULL IDENTITY,
        [TargetId] int NOT NULL,
        [ExternalId] nvarchar(64) NOT NULL,
        [OwaspCategory] nvarchar(128) NOT NULL,
        [Cwe] nvarchar(32) NOT NULL,
        [WstgId] nvarchar(32) NULL,
        [ExpectedSeverity] int NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_GroundTruthItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_GroundTruthItems_Targets_TargetId] FOREIGN KEY ([TargetId]) REFERENCES [Targets] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012103053_InitialCreate'
)
BEGIN
    CREATE TABLE [Scans] (
        [Id] int NOT NULL IDENTITY,
        [TargetId] int NOT NULL,
        [Profile] int NOT NULL,
        [Status] int NOT NULL,
        [StepBudget] int NOT NULL,
        [StepsUsed] int NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [StartedAt] datetimeoffset NULL,
        [CompletedAt] datetimeoffset NULL,
        CONSTRAINT [PK_Scans] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Scans_Targets_TargetId] FOREIGN KEY ([TargetId]) REFERENCES [Targets] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012103053_InitialCreate'
)
BEGIN
    CREATE TABLE [AuditLogEntries] (
        [Id] bigint NOT NULL IDENTITY,
        [ScanId] int NULL,
        [Timestamp] datetimeoffset NOT NULL,
        [Actor] int NOT NULL,
        [Action] nvarchar(256) NOT NULL,
        [DetailsJson] nvarchar(max) NULL,
        CONSTRAINT [PK_AuditLogEntries] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AuditLogEntries_Scans_ScanId] FOREIGN KEY ([ScanId]) REFERENCES [Scans] ([Id]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012103053_InitialCreate'
)
BEGIN
    CREATE TABLE [ToolCalls] (
        [Id] int NOT NULL IDENTITY,
        [ScanId] int NOT NULL,
        [ExternalCallId] nvarchar(64) NOT NULL,
        [ToolName] nvarchar(128) NOT NULL,
        [ToolKind] int NOT NULL,
        [ArgumentsJson] nvarchar(max) NOT NULL,
        [Status] int NOT NULL,
        [RefusalReason] nvarchar(max) NULL,
        [RawOutput] nvarchar(max) NULL,
        [NormalizedOutput] nvarchar(max) NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CompletedAt] datetimeoffset NULL,
        CONSTRAINT [PK_ToolCalls] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ToolCalls_Scans_ScanId] FOREIGN KEY ([ScanId]) REFERENCES [Scans] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012103053_InitialCreate'
)
BEGIN
    CREATE TABLE [Findings] (
        [Id] int NOT NULL IDENTITY,
        [ScanId] int NOT NULL,
        [ExternalId] nvarchar(64) NOT NULL,
        [Title] nvarchar(256) NOT NULL,
        [Cwe] nvarchar(32) NOT NULL,
        [OwaspCategory] nvarchar(128) NOT NULL,
        [WstgId] nvarchar(32) NULL,
        [Severity] int NOT NULL,
        [SeveritySource] int NOT NULL,
        [SeverityJustification] nvarchar(max) NULL,
        [Evidence] nvarchar(max) NOT NULL,
        [SourceToolCallId] int NULL,
        [AffectedUrlsJson] nvarchar(max) NOT NULL,
        [Remediation] nvarchar(max) NOT NULL,
        [Confidence] int NOT NULL,
        [IsHallucination] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_Findings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Findings_Scans_ScanId] FOREIGN KEY ([ScanId]) REFERENCES [Scans] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Findings_ToolCalls_SourceToolCallId] FOREIGN KEY ([SourceToolCallId]) REFERENCES [ToolCalls] ([Id]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012103053_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditLogEntries_ScanId] ON [AuditLogEntries] ([ScanId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012103053_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Findings_ScanId] ON [Findings] ([ScanId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012103053_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Findings_SourceToolCallId] ON [Findings] ([SourceToolCallId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012103053_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_GroundTruthItems_TargetId] ON [GroundTruthItems] ([TargetId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012103053_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Scans_TargetId] ON [Scans] ([TargetId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012103053_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Targets_Host] ON [Targets] ([Host]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012103053_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ToolCalls_ScanId_ExternalCallId] ON [ToolCalls] ([ScanId], [ExternalCallId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012103053_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261012103053_InitialCreate', N'8.0.11');
END;
GO

COMMIT;
GO

