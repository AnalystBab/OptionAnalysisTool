-- 🔐 ADD AUTHENTICATION TOKENS TABLE
-- Run this script in your SQL Server database to add token storage

USE [OptionAnalysisDB]  -- Replace with your database name
GO

-- Create AuthenticationTokens table if it doesn't exist
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
        [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    )
    
    PRINT '✅ AuthenticationTokens table created successfully'
END
ELSE
BEGIN
    PRINT 'ℹ️ AuthenticationTokens table already exists'
END
GO

-- Create indexes for performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AuthTokens_ApiKey_Active')
BEGIN
    CREATE INDEX [IX_AuthTokens_ApiKey_Active] ON [dbo].[AuthenticationTokens] 
    ([ApiKey] ASC, [IsActive] ASC)
    PRINT '✅ Index IX_AuthTokens_ApiKey_Active created'
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AuthTokens_Expiry_Active')
BEGIN
    CREATE INDEX [IX_AuthTokens_Expiry_Active] ON [dbo].[AuthenticationTokens] 
    ([ExpiresAt] ASC, [IsActive] ASC)
    PRINT '✅ Index IX_AuthTokens_Expiry_Active created'
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AuthTokens_CreatedAt')
BEGIN
    CREATE INDEX [IX_AuthTokens_CreatedAt] ON [dbo].[AuthenticationTokens] 
    ([CreatedAt] ASC)
    PRINT '✅ Index IX_AuthTokens_CreatedAt created'
END

PRINT '🎉 AuthenticationTokens setup complete!'
PRINT '📊 You can now store access tokens in the database'

-- Test query to verify table structure
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'AuthenticationTokens'
ORDER BY ORDINAL_POSITION 