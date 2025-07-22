USE PalindromeResults;

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AuthenticationTokens' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[AuthenticationTokens] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [AccessToken] NVARCHAR(500) NOT NULL,
        [ApiKey] NVARCHAR(100) NOT NULL,
        [ApiSecret] NVARCHAR(200) NULL,
        [UserId] NVARCHAR(100) NULL,
        [UserName] NVARCHAR(200) NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [ExpiresAt] DATETIME2 NOT NULL,
        [LastUsedAt] DATETIME2 NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [Source] NVARCHAR(50) NOT NULL DEFAULT 'Manual',
        [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [Metadata] NVARCHAR(MAX) NULL,
        [SourceIpAddress] NVARCHAR(50) NULL
    );
    
    CREATE INDEX [IX_AuthTokens_ApiKey_Active] ON [dbo].[AuthenticationTokens] ([ApiKey] ASC, [IsActive] ASC);
    CREATE INDEX [IX_AuthTokens_Expiry_Active] ON [dbo].[AuthenticationTokens] ([ExpiresAt] ASC, [IsActive] ASC);
    
    PRINT 'AuthenticationTokens table created successfully';
END
ELSE
BEGIN
    PRINT 'AuthenticationTokens table already exists';
END 