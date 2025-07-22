# 🔥 ADD EOD SYNC RESULT TABLE
# Creates table to track EOD data synchronization history

param(
    [string]$ConnectionString = "Server=LAPTOP-B68L4IP9;Database=PalindromeResults;Trusted_Connection=True;TrustServerCertificate=True;"
)

Write-Host "🔥 ADDING EOD SYNC RESULT TABLE" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host ""

try {
    # Load SQL Server module
    Import-Module SqlServer -ErrorAction SilentlyContinue
    
    Write-Host "📊 Creating EODSyncResult table..." -ForegroundColor Yellow
    
    # Read the SQL script
    $sqlScript = @"
-- Create EODSyncResult table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EODSyncResults')
BEGIN
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

    PRINT '✅ EODSyncResult table created successfully';
END
ELSE
BEGIN
    PRINT 'ℹ️ EODSyncResult table already exists';
END
"@

    # Execute the SQL script
    Invoke-Sqlcmd -ConnectionString $ConnectionString -Query $sqlScript
    
    Write-Host "✅ EODSyncResult table created successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📊 Table Features:" -ForegroundColor Yellow
    Write-Host "   • Tracks EOD sync history" -ForegroundColor White
    Write-Host "   • Records contracts processed" -ForegroundColor White
    Write-Host "   • Tracks success/error counts" -ForegroundColor White
    Write-Host "   • Stores sync duration" -ForegroundColor White
    Write-Host "   • Indexed for performance" -ForegroundColor White
    Write-Host ""
    Write-Host "🌅 Users can now:" -ForegroundColor Yellow
    Write-Host "   • Click 'EOD Data Sync' button" -ForegroundColor White
    Write-Host "   • Sync data up to latest traded date" -ForegroundColor White
    Write-Host "   • View sync history and results" -ForegroundColor White
    Write-Host "   • Track sync performance" -ForegroundColor White
    
}
catch {
    Write-Host "❌ Error creating EODSyncResult table: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Stack Trace: $($_.Exception.StackTrace)" -ForegroundColor Red
}

Write-Host ""
Write-Host "🎯 EOD Sync functionality is now ready!" -ForegroundColor Green 