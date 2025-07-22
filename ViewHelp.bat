@echo off
echo 📚 Opening Indian Option Analysis Tool Help Documentation...
echo.

if exist "help_content.html" (
    echo ✅ Opening HTML Help File...
    start "" "help_content.html"
) else if exist "OPTION_ANALYSIS_COMPREHENSIVE_HELP.md" (
    echo ✅ Opening Markdown Help File...
    start "" "OPTION_ANALYSIS_COMPREHENSIVE_HELP.md"
) else (
    echo ❌ Help files not found!
    echo Creating basic help information...
    echo.
    echo 🇮🇳 INDIAN OPTION ANALYSIS TOOL - QUICK HELP
    echo.
    echo 📅 DAILY ROUTINE (ONE MANUAL STEP):
    echo    8:45 AM: Run DailyAuth.bat
    echo    9:00 AM onwards: Everything automatic
    echo.
    echo 🖥️ DESKTOP WIDGET:
    echo    Run: StartEnhancedDesktopWidget.bat
    echo    Uses 25%% of desktop width (right side)
    echo    Real-time monitoring and status
    echo.
    echo 💾 DATABASE: PalindromeResults
    echo    Tables: IntradayOptionSnapshots, CircuitLimitTrackers
    echo    Real-time circuit limit monitoring
    echo.
    echo 🔧 TROUBLESHOOTING:
    echo    Check Service: CheckService.bat
    echo    Check Data: CheckLiveData.bat
    echo    Check Circuits: CheckCircuitLimitTracking.bat
    echo.
)

pause 