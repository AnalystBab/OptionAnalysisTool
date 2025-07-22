-- 🔥 ADD EOD SYNC RESULT TABLE
-- Creates table to track EOD data synchronization history

-- Create EODSyncResult table
CREATE TABLE EODSyncResults (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    TradingDate DATETIME2 NOT NULL,
    UnderlyingSymbol NVARCHAR(50) NOT NULL,
    ContractsProcessed INT NOT NULL DEFAULT 0,
    SuccessfulSaves INT NOT NULL DEFAULT 0,
    TotalErrors INT NOT NULL DEFAULT 0,
    Duration NVARCHAR(20) NOT NULL DEFAULT '',
    Status NVARCHAR(100) NOT NULL DEFAULT '',
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    LastUpdated DATETIME2 NOT NULL DEFAULT GETDATE()
);

-- Create indexes for performance
CREATE INDEX IX_EODSyncResult_TradingDate_UnderlyingSymbol 
ON EODSyncResults (TradingDate, UnderlyingSymbol);

CREATE INDEX IX_EODSyncResult_CreatedAt 
ON EODSyncResults (CreatedAt);

-- Add some sample data for testing
INSERT INTO EODSyncResults (TradingDate, UnderlyingSymbol, ContractsProcessed, SuccessfulSaves, TotalErrors, Duration, Status, CreatedAt, LastUpdated)
VALUES 
    (GETDATE(), 'ALL', 0, 0, 0, '00:00', 'Ready for sync', GETDATE(), GETDATE());

PRINT '✅ EODSyncResult table created successfully with indexes';
PRINT '📊 Table will track EOD data synchronization history';
PRINT '🌅 Users can now sync EOD data up to the latest traded date'; 