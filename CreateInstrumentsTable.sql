-- Create Instruments table manually
-- This script creates the Instruments table that should have been created by the migration

USE PalindromeResults;
GO

-- Check if table already exists
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Instruments]') AND type in (N'U'))
BEGIN
    -- Create the Instruments table
    CREATE TABLE [dbo].[Instruments](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [InstrumentToken] [nvarchar](50) NOT NULL,
        [ExchangeToken] [nvarchar](50) NOT NULL,
        [TradingSymbol] [nvarchar](50) NOT NULL,
        [Name] [nvarchar](50) NOT NULL,
        [Strike] [decimal](18,2) NOT NULL,
        [Expiry] [datetime2](7) NULL,
        [InstrumentType] [nvarchar](10) NOT NULL,
        [Segment] [nvarchar](20) NOT NULL,
        [Exchange] [nvarchar](20) NOT NULL,
        CONSTRAINT [PK_Instruments] PRIMARY KEY CLUSTERED 
        (
            [Id] ASC
        )
    );
    
    PRINT 'Instruments table created successfully!';
END
ELSE
BEGIN
    PRINT 'Instruments table already exists.';
END
GO

-- Verify the table was created
SELECT COUNT(*) as TableCount 
FROM sys.objects 
WHERE object_id = OBJECT_ID(N'[dbo].[Instruments]') AND type in (N'U');
GO 