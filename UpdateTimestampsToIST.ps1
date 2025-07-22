# 🔥 UPDATE TIMESTAMPS TO IST FORMAT
# Converts existing UTC timestamps to IST format (same as Kite API)

param(
    [string]$ConnectionString = "Server=LAPTOP-B68L4IP9;Database=PalindromeResults;Trusted_Connection=True;TrustServerCertificate=True;"
)

Write-Host "🔥 UPDATING TIMESTAMPS TO IST FORMAT" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "⚠️  WARNING: This will update all existing timestamps from UTC to IST format" -ForegroundColor Yellow
Write-Host "This is a one-time migration to match Kite API timestamp format" -ForegroundColor Yellow
Write-Host ""

$confirmation = Read-Host "Do you want to proceed? (y/N)"
if ($confirmation -ne "y" -and $confirmation -ne "Y") {
    Write-Host "❌ Update cancelled by user" -ForegroundColor Red
    exit
}

try {
    # Load SQL Server module
    Import-Module SqlServer -ErrorAction SilentlyContinue
    
    Write-Host "📊 Starting timestamp migration..." -ForegroundColor Green
    
    # Read the SQL script
    $sqlScript = Get-Content "UpdateTimestampsToIST.sql" -Raw
    
    Write-Host "🔄 Executing timestamp updates..." -ForegroundColor Yellow
    
    # Execute the update script
    $results = Invoke-Sqlcmd -ConnectionString $ConnectionString -Query $sqlScript
    
    Write-Host "✅ Timestamp migration completed successfully!" -ForegroundColor Green
    Write-Host ""
    
    # Display results
    Write-Host "📈 MIGRATION RESULTS:" -ForegroundColor Cyan
    $results | Format-Table -AutoSize
    
    Write-Host ""
    Write-Host "💡 KEY CHANGES MADE:" -ForegroundColor Cyan
    Write-Host "• All timestamps converted from UTC to IST (+5:30)" -ForegroundColor White
    Write-Host "• Timestamps now match Kite API format" -ForegroundColor White
    Write-Host "• Market hours detection will work correctly" -ForegroundColor White
    Write-Host "• Circuit limit changes will show correct IST times" -ForegroundColor White
    
    Write-Host ""
    Write-Host "🔍 VERIFICATION:" -ForegroundColor Cyan
    Write-Host "Run the following query to verify the changes:" -ForegroundColor White
    Write-Host "SELECT TOP 5 Id, TradingSymbol, Timestamp, CaptureTime FROM IntradayOptionSnapshots WHERE TradingSymbol = 'NIFTY2572424950CE' ORDER BY Timestamp DESC" -ForegroundColor Gray
    
}
catch {
    Write-Host "❌ Error during timestamp migration: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Stack Trace: $($_.Exception.StackTrace)" -ForegroundColor Red
}
finally {
    Write-Host ""
    Write-Host "🔧 Next Steps:" -ForegroundColor Yellow
    Write-Host "1. Restart the application to use new IST timestamps" -ForegroundColor White
    Write-Host "2. Verify that new data is stored in IST format" -ForegroundColor White
    Write-Host "3. Check that market hours detection works correctly" -ForegroundColor White
} 