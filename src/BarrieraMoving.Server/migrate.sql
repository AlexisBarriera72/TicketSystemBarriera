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
    WHERE [MigrationId] = N'20260718235013_InitialMovingSchema'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260718235013_InitialMovingSchema'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [DisplayName] nvarchar(max) NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(256) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260718235013_InitialMovingSchema'
)
BEGIN
    CREATE TABLE [Categories] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        CONSTRAINT [PK_Categories] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260718235013_InitialMovingSchema'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260718235013_InitialMovingSchema'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260718235013_InitialMovingSchema'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(128) NOT NULL,
        [ProviderKey] nvarchar(128) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260718235013_InitialMovingSchema'
)
BEGIN
    CREATE TABLE [AspNetUserPasskeys] (
        [CredentialId] varbinary(1024) NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [Data] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_AspNetUserPasskeys] PRIMARY KEY ([CredentialId]),
        CONSTRAINT [FK_AspNetUserPasskeys_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260718235013_InitialMovingSchema'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260718235013_InitialMovingSchema'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(128) NOT NULL,
        [Name] nvarchar(128) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260718235013_InitialMovingSchema'
)
BEGIN
    CREATE TABLE [RefreshTokens] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [TokenHash] nvarchar(450) NOT NULL,
        [ExpiresUtc] datetime2 NOT NULL,
        [CreatedUtc] datetime2 NOT NULL,
        [RevokedUtc] datetime2 NULL,
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RefreshTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260718235013_InitialMovingSchema'
)
BEGIN
    CREATE TABLE [Orders] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [Status] int NOT NULL,
        [Priority] int NOT NULL,
        [CategoryId] int NOT NULL,
        [AuthorId] nvarchar(450) NOT NULL,
        [AssignedDriverId] nvarchar(450) NULL,
        CONSTRAINT [PK_Orders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Orders_AspNetUsers_AssignedDriverId] FOREIGN KEY ([AssignedDriverId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Orders_AspNetUsers_AuthorId] FOREIGN KEY ([AuthorId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Orders_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260718235013_InitialMovingSchema'
)
BEGIN
    CREATE TABLE [Messages] (
        [Id] int NOT NULL IDENTITY,
        [Content] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [OrderId] int NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_Messages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Messages_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Messages_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260718235013_InitialMovingSchema'
)
BEGIN
    CREATE TABLE [TimeEntries] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [OrderId] int NULL,
        [ClockInUtc] datetime2 NOT NULL,
        [ClockOutUtc] datetime2 NULL,
        [ClockInLatitude] float NULL,
        [ClockInLongitude] float NULL,
        [ClockOutLatitude] float NULL,
        [ClockOutLongitude] float NULL,
        CONSTRAINT [PK_TimeEntries] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TimeEntries_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TimeEntries_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260718235013_InitialMovingSchema'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260718235013_InitialMovingSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260718235013_InitialMovingSchema'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260718235013_InitialMovingSchema'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260718235013_InitialMovingSchema'
)
BEGIN
    CREATE INDEX [IX_AspNetUserPasskeys_UserId] ON [AspNetUserPasskeys] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260718235013_InitialMovingSchema'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260718235013_InitialMovingSchema'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260718235013_InitialMovingSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260718235013_InitialMovingSchema'
)
BEGIN
    CREATE INDEX [IX_Messages_OrderId] ON [Messages] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260718235013_InitialMovingSchema'
)
BEGIN
    CREATE INDEX [IX_Messages_UserId] ON [Messages] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260718235013_InitialMovingSchema'
)
BEGIN
    CREATE INDEX [IX_Orders_AssignedDriverId] ON [Orders] ([AssignedDriverId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260718235013_InitialMovingSchema'
)
BEGIN
    CREATE INDEX [IX_Orders_AuthorId] ON [Orders] ([AuthorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260718235013_InitialMovingSchema'
)
BEGIN
    CREATE INDEX [IX_Orders_CategoryId] ON [Orders] ([CategoryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260718235013_InitialMovingSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RefreshTokens_TokenHash] ON [RefreshTokens] ([TokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260718235013_InitialMovingSchema'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260718235013_InitialMovingSchema'
)
BEGIN
    CREATE INDEX [IX_TimeEntries_OrderId] ON [TimeEntries] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260718235013_InitialMovingSchema'
)
BEGIN
    CREATE INDEX [IX_TimeEntries_UserId] ON [TimeEntries] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260718235013_InitialMovingSchema'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260718235013_InitialMovingSchema', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719074101_TimeEntryClockRules'
)
BEGIN
    DROP INDEX [IX_TimeEntries_UserId] ON [TimeEntries];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719074101_TimeEntryClockRules'
)
BEGIN
    ALTER TABLE [TimeEntries] ADD [AutoClosed] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719074101_TimeEntryClockRules'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_TimeEntries_OneOpenPerUser] ON [TimeEntries] ([UserId]) WHERE [ClockOutUtc] IS NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719074101_TimeEntryClockRules'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260719074101_TimeEntryClockRules', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719172127_MessageSenderRole'
)
BEGIN
    ALTER TABLE [Messages] ADD [IsSystem] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719172127_MessageSenderRole'
)
BEGIN
    ALTER TABLE [Messages] ADD [SenderRole] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719172127_MessageSenderRole'
)
BEGIN
    UPDATE Messages SET IsSystem = 1 WHERE Content LIKE '[[]%'
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260719172127_MessageSenderRole'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260719172127_MessageSenderRole', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720024450_PhotoAttachmentsAndOutbox'
)
BEGIN
    ALTER TABLE [TimeEntries] ADD [ClockInCapturedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720024450_PhotoAttachmentsAndOutbox'
)
BEGIN
    ALTER TABLE [TimeEntries] ADD [ClockInIdempotencyKey] nvarchar(450) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720024450_PhotoAttachmentsAndOutbox'
)
BEGIN
    ALTER TABLE [TimeEntries] ADD [ClockOutCapturedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720024450_PhotoAttachmentsAndOutbox'
)
BEGIN
    ALTER TABLE [TimeEntries] ADD [ClockOutIdempotencyKey] nvarchar(450) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720024450_PhotoAttachmentsAndOutbox'
)
BEGIN
    ALTER TABLE [Messages] ADD [AttachmentPath] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720024450_PhotoAttachmentsAndOutbox'
)
BEGIN
    ALTER TABLE [Messages] ADD [AttachmentThumbPath] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720024450_PhotoAttachmentsAndOutbox'
)
BEGIN
    ALTER TABLE [Messages] ADD [CapturedAtUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720024450_PhotoAttachmentsAndOutbox'
)
BEGIN
    ALTER TABLE [Messages] ADD [IdempotencyKey] nvarchar(450) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720024450_PhotoAttachmentsAndOutbox'
)
BEGIN
    ALTER TABLE [Messages] ADD [Latitude] float NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720024450_PhotoAttachmentsAndOutbox'
)
BEGIN
    ALTER TABLE [Messages] ADD [Longitude] float NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720024450_PhotoAttachmentsAndOutbox'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_TimeEntries_ClockInIdempotencyKey] ON [TimeEntries] ([ClockInIdempotencyKey]) WHERE [ClockInIdempotencyKey] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720024450_PhotoAttachmentsAndOutbox'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_TimeEntries_ClockOutIdempotencyKey] ON [TimeEntries] ([ClockOutIdempotencyKey]) WHERE [ClockOutIdempotencyKey] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720024450_PhotoAttachmentsAndOutbox'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Messages_IdempotencyKey] ON [Messages] ([IdempotencyKey]) WHERE [IdempotencyKey] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260720024450_PhotoAttachmentsAndOutbox'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260720024450_PhotoAttachmentsAndOutbox', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721162458_SignatureDocuments'
)
BEGIN
    CREATE TABLE [SignatureDocuments] (
        [Id] int NOT NULL IDENTITY,
        [OrderId] int NOT NULL,
        [RequestedByUserId] nvarchar(450) NOT NULL,
        [Status] int NOT NULL,
        [IsProvisional] bit NOT NULL,
        [ProviderEnvelopeId] nvarchar(450) NULL,
        [SignerName] nvarchar(max) NULL,
        [SignerEmail] nvarchar(max) NULL,
        [Latitude] float NULL,
        [Longitude] float NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [SignedAtUtc] datetime2 NULL,
        [SignedCapturedAtUtc] datetime2 NULL,
        [PdfPath] nvarchar(max) NULL,
        [ContentHash] nvarchar(max) NULL,
        [ReviewedByUserId] nvarchar(450) NULL,
        [ReviewedAtUtc] datetime2 NULL,
        [RejectReason] nvarchar(max) NULL,
        [EmailStatus] int NOT NULL,
        [EmailError] nvarchar(max) NULL,
        [IdempotencyKey] nvarchar(450) NULL,
        CONSTRAINT [PK_SignatureDocuments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SignatureDocuments_AspNetUsers_RequestedByUserId] FOREIGN KEY ([RequestedByUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SignatureDocuments_AspNetUsers_ReviewedByUserId] FOREIGN KEY ([ReviewedByUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SignatureDocuments_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721162458_SignatureDocuments'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_SignatureDocuments_IdempotencyKey] ON [SignatureDocuments] ([IdempotencyKey]) WHERE [IdempotencyKey] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721162458_SignatureDocuments'
)
BEGIN
    CREATE INDEX [IX_SignatureDocuments_OrderId] ON [SignatureDocuments] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721162458_SignatureDocuments'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_SignatureDocuments_ProviderEnvelopeId] ON [SignatureDocuments] ([ProviderEnvelopeId]) WHERE [ProviderEnvelopeId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721162458_SignatureDocuments'
)
BEGIN
    CREATE INDEX [IX_SignatureDocuments_RequestedByUserId] ON [SignatureDocuments] ([RequestedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721162458_SignatureDocuments'
)
BEGIN
    CREATE INDEX [IX_SignatureDocuments_ReviewedByUserId] ON [SignatureDocuments] ([ReviewedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721162458_SignatureDocuments'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260721162458_SignatureDocuments', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721193608_PaperworkDocuments'
)
BEGIN
    CREATE TABLE [PaperworkDocuments] (
        [Id] int NOT NULL IDENTITY,
        [OrderId] int NOT NULL,
        [SlotKey] nvarchar(450) NOT NULL,
        [UploadedByUserId] nvarchar(450) NOT NULL,
        [FilePath] nvarchar(max) NOT NULL,
        [ThumbPath] nvarchar(max) NULL,
        [IsPdf] bit NOT NULL,
        [ContentHash] nvarchar(max) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [CapturedAtUtc] datetime2 NULL,
        [Latitude] float NULL,
        [Longitude] float NULL,
        [Status] int NOT NULL,
        [RejectReason] nvarchar(max) NULL,
        [ReviewedByUserId] nvarchar(450) NULL,
        [ReviewedAtUtc] datetime2 NULL,
        [IdempotencyKey] nvarchar(450) NULL,
        CONSTRAINT [PK_PaperworkDocuments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PaperworkDocuments_AspNetUsers_ReviewedByUserId] FOREIGN KEY ([ReviewedByUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PaperworkDocuments_AspNetUsers_UploadedByUserId] FOREIGN KEY ([UploadedByUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PaperworkDocuments_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721193608_PaperworkDocuments'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_PaperworkDocuments_IdempotencyKey] ON [PaperworkDocuments] ([IdempotencyKey]) WHERE [IdempotencyKey] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721193608_PaperworkDocuments'
)
BEGIN
    CREATE INDEX [IX_PaperworkDocuments_OrderId_SlotKey] ON [PaperworkDocuments] ([OrderId], [SlotKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721193608_PaperworkDocuments'
)
BEGIN
    CREATE INDEX [IX_PaperworkDocuments_ReviewedByUserId] ON [PaperworkDocuments] ([ReviewedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721193608_PaperworkDocuments'
)
BEGIN
    CREATE INDEX [IX_PaperworkDocuments_UploadedByUserId] ON [PaperworkDocuments] ([UploadedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721193608_PaperworkDocuments'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260721193608_PaperworkDocuments', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721194518_DirectMessaging'
)
BEGIN
    CREATE TABLE [DirectConversations] (
        [Id] int NOT NULL IDENTITY,
        [CreatedAtUtc] datetime2 NOT NULL,
        [CreatedByUserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_DirectConversations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DirectConversations_AspNetUsers_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721194518_DirectMessaging'
)
BEGIN
    CREATE TABLE [DirectMessages] (
        [Id] int NOT NULL IDENTITY,
        [ConversationId] int NOT NULL,
        [SenderUserId] nvarchar(450) NOT NULL,
        [Content] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CapturedAtUtc] datetime2 NULL,
        [SenderRole] nvarchar(max) NULL,
        [IdempotencyKey] nvarchar(450) NULL,
        CONSTRAINT [PK_DirectMessages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DirectMessages_AspNetUsers_SenderUserId] FOREIGN KEY ([SenderUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DirectMessages_DirectConversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [DirectConversations] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721194518_DirectMessaging'
)
BEGIN
    CREATE TABLE [DirectParticipants] (
        [Id] int NOT NULL IDENTITY,
        [ConversationId] int NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_DirectParticipants] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DirectParticipants_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DirectParticipants_DirectConversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [DirectConversations] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721194518_DirectMessaging'
)
BEGIN
    CREATE INDEX [IX_DirectConversations_CreatedByUserId] ON [DirectConversations] ([CreatedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721194518_DirectMessaging'
)
BEGIN
    CREATE INDEX [IX_DirectMessages_ConversationId] ON [DirectMessages] ([ConversationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721194518_DirectMessaging'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_DirectMessages_IdempotencyKey] ON [DirectMessages] ([IdempotencyKey]) WHERE [IdempotencyKey] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721194518_DirectMessaging'
)
BEGIN
    CREATE INDEX [IX_DirectMessages_SenderUserId] ON [DirectMessages] ([SenderUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721194518_DirectMessaging'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DirectParticipants_ConversationId_UserId] ON [DirectParticipants] ([ConversationId], [UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721194518_DirectMessaging'
)
BEGIN
    CREATE INDEX [IX_DirectParticipants_UserId] ON [DirectParticipants] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721194518_DirectMessaging'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260721194518_DirectMessaging', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721222146_Complaints'
)
BEGIN
    CREATE TABLE [Complaints] (
        [Id] int NOT NULL IDENTITY,
        [ClientUserId] nvarchar(450) NOT NULL,
        [OrderId] int NULL,
        [Subject] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Status] int NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [OfficeResponse] nvarchar(max) NULL,
        [RespondedByUserId] nvarchar(450) NULL,
        [RespondedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_Complaints] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Complaints_AspNetUsers_ClientUserId] FOREIGN KEY ([ClientUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Complaints_AspNetUsers_RespondedByUserId] FOREIGN KEY ([RespondedByUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Complaints_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721222146_Complaints'
)
BEGIN
    CREATE INDEX [IX_Complaints_ClientUserId] ON [Complaints] ([ClientUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721222146_Complaints'
)
BEGIN
    CREATE INDEX [IX_Complaints_OrderId] ON [Complaints] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721222146_Complaints'
)
BEGIN
    CREATE INDEX [IX_Complaints_RespondedByUserId] ON [Complaints] ([RespondedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721222146_Complaints'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260721222146_Complaints', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723210334_PushDeviceTokens'
)
BEGIN
    CREATE TABLE [DeviceTokens] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [Token] nvarchar(450) NOT NULL,
        [Platform] nvarchar(max) NOT NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [LastSeenUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_DeviceTokens] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723210334_PushDeviceTokens'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DeviceTokens_Token] ON [DeviceTokens] ([Token]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723210334_PushDeviceTokens'
)
BEGIN
    CREATE INDEX [IX_DeviceTokens_UserId] ON [DeviceTokens] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723210334_PushDeviceTokens'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260723210334_PushDeviceTokens', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724174918_QuoteRequests'
)
BEGIN
    CREATE TABLE [QuoteRequests] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Phone] nvarchar(max) NOT NULL,
        [Email] nvarchar(max) NULL,
        [ServiceType] nvarchar(max) NULL,
        [OriginZone] nvarchar(max) NULL,
        [DestinationZone] nvarchar(max) NULL,
        [PreferredDate] nvarchar(max) NULL,
        [Details] nvarchar(max) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [Handled] bit NOT NULL,
        CONSTRAINT [PK_QuoteRequests] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724174918_QuoteRequests'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260724174918_QuoteRequests', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725010829_StringLengthLimits'
)
BEGIN
    DROP INDEX [IX_TimeEntries_ClockOutIdempotencyKey] ON [TimeEntries];
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TimeEntries]') AND [c].[name] = N'ClockOutIdempotencyKey');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [TimeEntries] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [TimeEntries] ALTER COLUMN [ClockOutIdempotencyKey] nvarchar(64) NULL;
    EXEC(N'CREATE UNIQUE INDEX [IX_TimeEntries_ClockOutIdempotencyKey] ON [TimeEntries] ([ClockOutIdempotencyKey]) WHERE [ClockOutIdempotencyKey] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725010829_StringLengthLimits'
)
BEGIN
    DROP INDEX [IX_TimeEntries_ClockInIdempotencyKey] ON [TimeEntries];
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TimeEntries]') AND [c].[name] = N'ClockInIdempotencyKey');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [TimeEntries] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [TimeEntries] ALTER COLUMN [ClockInIdempotencyKey] nvarchar(64) NULL;
    EXEC(N'CREATE UNIQUE INDEX [IX_TimeEntries_ClockInIdempotencyKey] ON [TimeEntries] ([ClockInIdempotencyKey]) WHERE [ClockInIdempotencyKey] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725010829_StringLengthLimits'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SignatureDocuments]') AND [c].[name] = N'SignerName');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [SignatureDocuments] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [SignatureDocuments] ALTER COLUMN [SignerName] nvarchar(120) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725010829_StringLengthLimits'
)
BEGIN
    DECLARE @var3 nvarchar(max);
    SELECT @var3 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SignatureDocuments]') AND [c].[name] = N'SignerEmail');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [SignatureDocuments] DROP CONSTRAINT ' + @var3 + ';');
    ALTER TABLE [SignatureDocuments] ALTER COLUMN [SignerEmail] nvarchar(256) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725010829_StringLengthLimits'
)
BEGIN
    DECLARE @var4 nvarchar(max);
    SELECT @var4 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SignatureDocuments]') AND [c].[name] = N'RejectReason');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [SignatureDocuments] DROP CONSTRAINT ' + @var4 + ';');
    ALTER TABLE [SignatureDocuments] ALTER COLUMN [RejectReason] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725010829_StringLengthLimits'
)
BEGIN
    DROP INDEX [IX_SignatureDocuments_ProviderEnvelopeId] ON [SignatureDocuments];
    DECLARE @var5 nvarchar(max);
    SELECT @var5 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SignatureDocuments]') AND [c].[name] = N'ProviderEnvelopeId');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [SignatureDocuments] DROP CONSTRAINT ' + @var5 + ';');
    ALTER TABLE [SignatureDocuments] ALTER COLUMN [ProviderEnvelopeId] nvarchar(200) NULL;
    EXEC(N'CREATE UNIQUE INDEX [IX_SignatureDocuments_ProviderEnvelopeId] ON [SignatureDocuments] ([ProviderEnvelopeId]) WHERE [ProviderEnvelopeId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725010829_StringLengthLimits'
)
BEGIN
    DECLARE @var6 nvarchar(max);
    SELECT @var6 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SignatureDocuments]') AND [c].[name] = N'PdfPath');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [SignatureDocuments] DROP CONSTRAINT ' + @var6 + ';');
    ALTER TABLE [SignatureDocuments] ALTER COLUMN [PdfPath] nvarchar(400) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725010829_StringLengthLimits'
)
BEGIN
    DROP INDEX [IX_SignatureDocuments_IdempotencyKey] ON [SignatureDocuments];
    DECLARE @var7 nvarchar(max);
    SELECT @var7 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SignatureDocuments]') AND [c].[name] = N'IdempotencyKey');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [SignatureDocuments] DROP CONSTRAINT ' + @var7 + ';');
    ALTER TABLE [SignatureDocuments] ALTER COLUMN [IdempotencyKey] nvarchar(64) NULL;
    EXEC(N'CREATE UNIQUE INDEX [IX_SignatureDocuments_IdempotencyKey] ON [SignatureDocuments] ([IdempotencyKey]) WHERE [IdempotencyKey] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725010829_StringLengthLimits'
)
BEGIN
    DECLARE @var8 nvarchar(max);
    SELECT @var8 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SignatureDocuments]') AND [c].[name] = N'EmailError');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [SignatureDocuments] DROP CONSTRAINT ' + @var8 + ';');
    ALTER TABLE [SignatureDocuments] ALTER COLUMN [EmailError] nvarchar(2000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725010829_StringLengthLimits'
)
BEGIN
    DECLARE @var9 nvarchar(max);
    SELECT @var9 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SignatureDocuments]') AND [c].[name] = N'ContentHash');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [SignatureDocuments] DROP CONSTRAINT ' + @var9 + ';');
    ALTER TABLE [SignatureDocuments] ALTER COLUMN [ContentHash] nvarchar(128) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725010829_StringLengthLimits'
)
BEGIN
    DROP INDEX [IX_RefreshTokens_TokenHash] ON [RefreshTokens];
    DECLARE @var10 nvarchar(max);
    SELECT @var10 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RefreshTokens]') AND [c].[name] = N'TokenHash');
    IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [RefreshTokens] DROP CONSTRAINT ' + @var10 + ';');
    ALTER TABLE [RefreshTokens] ALTER COLUMN [TokenHash] nvarchar(128) NOT NULL;
    CREATE UNIQUE INDEX [IX_RefreshTokens_TokenHash] ON [RefreshTokens] ([TokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725010829_StringLengthLimits'
)
BEGIN
    DECLARE @var11 nvarchar(max);
    SELECT @var11 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[QuoteRequests]') AND [c].[name] = N'ServiceType');
    IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [QuoteRequests] DROP CONSTRAINT ' + @var11 + ';');
    ALTER TABLE [QuoteRequests] ALTER COLUMN [ServiceType] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725010829_StringLengthLimits'
)
BEGIN
    DECLARE @var12 nvarchar(max);
    SELECT @var12 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[QuoteRequests]') AND [c].[name] = N'PreferredDate');
    IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [QuoteRequests] DROP CONSTRAINT ' + @var12 + ';');
    ALTER TABLE [QuoteRequests] ALTER COLUMN [PreferredDate] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725010829_StringLengthLimits'
)
BEGIN
    DECLARE @var13 nvarchar(max);
    SELECT @var13 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[QuoteRequests]') AND [c].[name] = N'Phone');
    IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [QuoteRequests] DROP CONSTRAINT ' + @var13 + ';');
    ALTER TABLE [QuoteRequests] ALTER COLUMN [Phone] nvarchar(40) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725010829_StringLengthLimits'
)
BEGIN
    DECLARE @var14 nvarchar(max);
    SELECT @var14 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[QuoteRequests]') AND [c].[name] = N'OriginZone');
    IF @var14 IS NOT NULL EXEC(N'ALTER TABLE [QuoteRequests] DROP CONSTRAINT ' + @var14 + ';');
    ALTER TABLE [QuoteRequests] ALTER COLUMN [OriginZone] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725010829_StringLengthLimits'
)
BEGIN
    DECLARE @var15 nvarchar(max);
    SELECT @var15 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[QuoteRequests]') AND [c].[name] = N'Name');
    IF @var15 IS NOT NULL EXEC(N'ALTER TABLE [QuoteRequests] DROP CONSTRAINT ' + @var15 + ';');
    ALTER TABLE [QuoteRequests] ALTER COLUMN [Name] nvarchar(120) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725010829_StringLengthLimits'
)
BEGIN
    DECLARE @var16 nvarchar(max);
    SELECT @var16 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[QuoteRequests]') AND [c].[name] = N'Email');
    IF @var16 IS NOT NULL EXEC(N'ALTER TABLE [QuoteRequests] DROP CONSTRAINT ' + @var16 + ';');
    ALTER TABLE [QuoteRequests] ALTER COLUMN [Email] nvarchar(256) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725010829_StringLengthLimits'
)
BEGIN
    DECLARE @var17 nvarchar(max);
    SELECT @var17 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[QuoteRequests]') AND [c].[name] = N'Details');
    IF @var17 IS NOT NULL EXEC(N'ALTER TABLE [QuoteRequests] DROP CONSTRAINT ' + @var17 + ';');
    ALTER TABLE [QuoteRequests] ALTER COLUMN [Details] nvarchar(2000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725010829_StringLengthLimits'
)
BEGIN
    DECLARE @var18 nvarchar(max);
    SELECT @var18 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[QuoteRequests]') AND [c].[name] = N'DestinationZone');
    IF @var18 IS NOT NULL EXEC(N'ALTER TABLE [QuoteRequests] DROP CONSTRAINT ' + @var18 + ';');
    ALTER TABLE [QuoteRequests] ALTER COLUMN [DestinationZone] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725010829_StringLengthLimits'
)
BEGIN
    DECLARE @var19 nvarchar(max);
    SELECT @var19 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PaperworkDocuments]') AND [c].[name] = N'ThumbPath');
    IF @var19 IS NOT NULL EXEC(N'ALTER TABLE [PaperworkDocuments] DROP CONSTRAINT ' + @var19 + ';');
    ALTER TABLE [PaperworkDocuments] ALTER COLUMN [ThumbPath] nvarchar(400) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725010829_StringLengthLimits'
)
BEGIN
    DROP INDEX [IX_PaperworkDocuments_OrderId_SlotKey] ON [PaperworkDocuments];
    DECLARE @var20 nvarchar(max);
    SELECT @var20 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PaperworkDocuments]') AND [c].[name] = N'SlotKey');
    IF @var20 IS NOT NULL EXEC(N'ALTER TABLE [PaperworkDocuments] DROP CONSTRAINT ' + @var20 + ';');
    ALTER TABLE [PaperworkDocuments] ALTER COLUMN [SlotKey] nvarchar(100) NOT NULL;
    CREATE INDEX [IX_PaperworkDocuments_OrderId_SlotKey] ON [PaperworkDocuments] ([OrderId], [SlotKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725010829_StringLengthLimits'
)
BEGIN
    DECLARE @var21 nvarchar(max);
    SELECT @var21 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PaperworkDocuments]') AND [c].[name] = N'RejectReason');
    IF @var21 IS NOT NULL EXEC(N'ALTER TABLE [PaperworkDocuments] DROP CONSTRAINT ' + @var21 + ';');
    ALTER TABLE [PaperworkDocuments] ALTER COLUMN [RejectReason] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725010829_StringLengthLimits'
)
BEGIN
    DROP INDEX [IX_PaperworkDocuments_IdempotencyKey] ON [PaperworkDocuments];
    DECLARE @var22 nvarchar(max);
    SELECT @var22 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PaperworkDocuments]') AND [c].[name] = N'IdempotencyKey');
    IF @var22 IS NOT NULL EXEC(N'ALTER TABLE [PaperworkDocuments] DROP CONSTRAINT ' + @var22 + ';');
    ALTER TABLE [PaperworkDocuments] ALTER COLUMN [IdempotencyKey] nvarchar(64) NULL;
    EXEC(N'CREATE UNIQUE INDEX [IX_PaperworkDocuments_IdempotencyKey] ON [PaperworkDocuments] ([IdempotencyKey]) WHERE [IdempotencyKey] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725010829_StringLengthLimits'
)
BEGIN
    DECLARE @var23 nvarchar(max);
    SELECT @var23 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PaperworkDocuments]') AND [c].[name] = N'FilePath');
    IF @var23 IS NOT NULL EXEC(N'ALTER TABLE [PaperworkDocuments] DROP CONSTRAINT ' + @var23 + ';');
    ALTER TABLE [PaperworkDocuments] ALTER COLUMN [FilePath] nvarchar(400) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725010829_StringLengthLimits'
)
BEGIN
    DECLARE @var24 nvarchar(max);
    SELECT @var24 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PaperworkDocuments]') AND [c].[name] = N'ContentHash');
    IF @var24 IS NOT NULL EXEC(N'ALTER TABLE [PaperworkDocuments] DROP CONSTRAINT ' + @var24 + ';');
    ALTER TABLE [PaperworkDocuments] ALTER COLUMN [ContentHash] nvarchar(128) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725010829_StringLengthLimits'
)
BEGIN
    DECLARE @var25 nvarchar(max);
    SELECT @var25 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Orders]') AND [c].[name] = N'Title');
    IF @var25 IS NOT NULL EXEC(N'ALTER TABLE [Orders] DROP CONSTRAINT ' + @var25 + ';');
    ALTER TABLE [Orders] ALTER COLUMN [Title] nvarchar(200) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725010829_StringLengthLimits'
)
BEGIN
    DECLARE @var26 nvarchar(max);
    SELECT @var26 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Orders]') AND [c].[name] = N'Description');
    IF @var26 IS NOT NULL EXEC(N'ALTER TABLE [Orders] DROP CONSTRAINT ' + @var26 + ';');
    ALTER TABLE [Orders] ALTER COLUMN [Description] nvarchar(2000) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725010829_StringLengthLimits'
)
BEGIN
    DECLARE @var27 nvarchar(max);
    SELECT @var27 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Messages]') AND [c].[name] = N'SenderRole');
    IF @var27 IS NOT NULL EXEC(N'ALTER TABLE [Messages] DROP CONSTRAINT ' + @var27 + ';');
    ALTER TABLE [Messages] ALTER COLUMN [SenderRole] nvarchar(32) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725010829_StringLengthLimits'
)
BEGIN
    DROP INDEX [IX_Messages_IdempotencyKey] ON [Messages];
    DECLARE @var28 nvarchar(max);
    SELECT @var28 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Messages]') AND [c].[name] = N'IdempotencyKey');
    IF @var28 IS NOT NULL EXEC(N'ALTER TABLE [Messages] DROP CONSTRAINT ' + @var28 + ';');
    ALTER TABLE [Messages] ALTER COLUMN [IdempotencyKey] nvarchar(64) NULL;
    EXEC(N'CREATE UNIQUE INDEX [IX_Messages_IdempotencyKey] ON [Messages] ([IdempotencyKey]) WHERE [IdempotencyKey] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725010829_StringLengthLimits'
)
BEGIN
    DECLARE @var29 nvarchar(max);
    SELECT @var29 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Messages]') AND [c].[name] = N'Content');
    IF @var29 IS NOT NULL EXEC(N'ALTER TABLE [Messages] DROP CONSTRAINT ' + @var29 + ';');
    ALTER TABLE [Messages] ALTER COLUMN [Content] nvarchar(2000) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725010829_StringLengthLimits'
)
BEGIN
    DECLARE @var30 nvarchar(max);
    SELECT @var30 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Messages]') AND [c].[name] = N'AttachmentThumbPath');
    IF @var30 IS NOT NULL EXEC(N'ALTER TABLE [Messages] DROP CONSTRAINT ' + @var30 + ';');
    ALTER TABLE [Messages] ALTER COLUMN [AttachmentThumbPath] nvarchar(400) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725010829_StringLengthLimits'
)
BEGIN
    DECLARE @var31 nvarchar(max);
    SELECT @var31 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Messages]') AND [c].[name] = N'AttachmentPath');
    IF @var31 IS NOT NULL EXEC(N'ALTER TABLE [Messages] DROP CONSTRAINT ' + @var31 + ';');
    ALTER TABLE [Messages] ALTER COLUMN [AttachmentPath] nvarchar(400) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725010829_StringLengthLimits'
)
BEGIN
    DECLARE @var32 nvarchar(max);
    SELECT @var32 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[DirectMessages]') AND [c].[name] = N'SenderRole');
    IF @var32 IS NOT NULL EXEC(N'ALTER TABLE [DirectMessages] DROP CONSTRAINT ' + @var32 + ';');
    ALTER TABLE [DirectMessages] ALTER COLUMN [SenderRole] nvarchar(32) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725010829_StringLengthLimits'
)
BEGIN
    DROP INDEX [IX_DirectMessages_IdempotencyKey] ON [DirectMessages];
    DECLARE @var33 nvarchar(max);
    SELECT @var33 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[DirectMessages]') AND [c].[name] = N'IdempotencyKey');
    IF @var33 IS NOT NULL EXEC(N'ALTER TABLE [DirectMessages] DROP CONSTRAINT ' + @var33 + ';');
    ALTER TABLE [DirectMessages] ALTER COLUMN [IdempotencyKey] nvarchar(64) NULL;
    EXEC(N'CREATE UNIQUE INDEX [IX_DirectMessages_IdempotencyKey] ON [DirectMessages] ([IdempotencyKey]) WHERE [IdempotencyKey] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725010829_StringLengthLimits'
)
BEGIN
    DECLARE @var34 nvarchar(max);
    SELECT @var34 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[DirectMessages]') AND [c].[name] = N'Content');
    IF @var34 IS NOT NULL EXEC(N'ALTER TABLE [DirectMessages] DROP CONSTRAINT ' + @var34 + ';');
    ALTER TABLE [DirectMessages] ALTER COLUMN [Content] nvarchar(2000) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725010829_StringLengthLimits'
)
BEGIN
    DROP INDEX [IX_DeviceTokens_Token] ON [DeviceTokens];
    DECLARE @var35 nvarchar(max);
    SELECT @var35 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[DeviceTokens]') AND [c].[name] = N'Token');
    IF @var35 IS NOT NULL EXEC(N'ALTER TABLE [DeviceTokens] DROP CONSTRAINT ' + @var35 + ';');
    ALTER TABLE [DeviceTokens] ALTER COLUMN [Token] nvarchar(500) NOT NULL;
    CREATE UNIQUE INDEX [IX_DeviceTokens_Token] ON [DeviceTokens] ([Token]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725010829_StringLengthLimits'
)
BEGIN
    DECLARE @var36 nvarchar(max);
    SELECT @var36 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[DeviceTokens]') AND [c].[name] = N'Platform');
    IF @var36 IS NOT NULL EXEC(N'ALTER TABLE [DeviceTokens] DROP CONSTRAINT ' + @var36 + ';');
    ALTER TABLE [DeviceTokens] ALTER COLUMN [Platform] nvarchar(32) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725010829_StringLengthLimits'
)
BEGIN
    DECLARE @var37 nvarchar(max);
    SELECT @var37 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Complaints]') AND [c].[name] = N'Subject');
    IF @var37 IS NOT NULL EXEC(N'ALTER TABLE [Complaints] DROP CONSTRAINT ' + @var37 + ';');
    ALTER TABLE [Complaints] ALTER COLUMN [Subject] nvarchar(200) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725010829_StringLengthLimits'
)
BEGIN
    DECLARE @var38 nvarchar(max);
    SELECT @var38 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Complaints]') AND [c].[name] = N'OfficeResponse');
    IF @var38 IS NOT NULL EXEC(N'ALTER TABLE [Complaints] DROP CONSTRAINT ' + @var38 + ';');
    ALTER TABLE [Complaints] ALTER COLUMN [OfficeResponse] nvarchar(2000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725010829_StringLengthLimits'
)
BEGIN
    DECLARE @var39 nvarchar(max);
    SELECT @var39 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Complaints]') AND [c].[name] = N'Description');
    IF @var39 IS NOT NULL EXEC(N'ALTER TABLE [Complaints] DROP CONSTRAINT ' + @var39 + ';');
    ALTER TABLE [Complaints] ALTER COLUMN [Description] nvarchar(2000) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725010829_StringLengthLimits'
)
BEGIN
    DECLARE @var40 nvarchar(max);
    SELECT @var40 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Categories]') AND [c].[name] = N'Name');
    IF @var40 IS NOT NULL EXEC(N'ALTER TABLE [Categories] DROP CONSTRAINT ' + @var40 + ';');
    ALTER TABLE [Categories] ALTER COLUMN [Name] nvarchar(200) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725010829_StringLengthLimits'
)
BEGIN
    DECLARE @var41 nvarchar(max);
    SELECT @var41 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Categories]') AND [c].[name] = N'Description');
    IF @var41 IS NOT NULL EXEC(N'ALTER TABLE [Categories] DROP CONSTRAINT ' + @var41 + ';');
    ALTER TABLE [Categories] ALTER COLUMN [Description] nvarchar(2000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725010829_StringLengthLimits'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260725010829_StringLengthLimits', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725011944_QuoteToOrderLink'
)
BEGIN
    ALTER TABLE [QuoteRequests] ADD [ConvertedOrderId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725011944_QuoteToOrderLink'
)
BEGIN
    CREATE INDEX [IX_QuoteRequests_ConvertedOrderId] ON [QuoteRequests] ([ConvertedOrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725011944_QuoteToOrderLink'
)
BEGIN
    ALTER TABLE [QuoteRequests] ADD CONSTRAINT [FK_QuoteRequests_Orders_ConvertedOrderId] FOREIGN KEY ([ConvertedOrderId]) REFERENCES [Orders] ([Id]) ON DELETE SET NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725011944_QuoteToOrderLink'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260725011944_QuoteToOrderLink', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725012513_OrderTrackingToken'
)
BEGIN
    ALTER TABLE [Orders] ADD [TrackingToken] nvarchar(64) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725012513_OrderTrackingToken'
)
BEGIN
    ALTER TABLE [Orders] ADD [TrackingTokenCreatedUtc] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725012513_OrderTrackingToken'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Orders_TrackingToken] ON [Orders] ([TrackingToken]) WHERE [TrackingToken] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725012513_OrderTrackingToken'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260725012513_OrderTrackingToken', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725013111_PhotoStages'
)
BEGIN
    ALTER TABLE [Messages] ADD [Stage] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725013111_PhotoStages'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260725013111_PhotoStages', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725015031_UserDisplayNameLength'
)
BEGIN
    DECLARE @var42 nvarchar(max);
    SELECT @var42 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetUsers]') AND [c].[name] = N'DisplayName');
    IF @var42 IS NOT NULL EXEC(N'ALTER TABLE [AspNetUsers] DROP CONSTRAINT ' + @var42 + ';');
    ALTER TABLE [AspNetUsers] ALTER COLUMN [DisplayName] nvarchar(120) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725015031_UserDisplayNameLength'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260725015031_UserDisplayNameLength', N'10.0.10');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725015621_GuestReferenceCode'
)
BEGIN
    ALTER TABLE [QuoteRequests] ADD [ReferenceCode] nvarchar(32) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725015621_GuestReferenceCode'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_QuoteRequests_ReferenceCode] ON [QuoteRequests] ([ReferenceCode]) WHERE [ReferenceCode] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260725015621_GuestReferenceCode'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260725015621_GuestReferenceCode', N'10.0.10');
END;

COMMIT;
GO

