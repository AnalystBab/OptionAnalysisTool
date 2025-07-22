@echo off
echo.
echo 🌅 INDIAN OPTION ANALYSIS - DAILY AUTHENTICATION
echo ===============================================
echo 🔐 Step 1: Get access token and store in database
echo 🚀 Step 2: Auto-start data collection service
echo.
echo ⏱️  Time required: 1-2 minutes
echo 🎯 Run this every morning at 9:00 AM
echo.

REM Change to the correct directory
cd /d "%~dp0"

REM Run the authentication tool
echo 🔄 Starting authentication process...
echo 💾 This will store your token in the database for ALL services to use
echo.
dotnet run --project SimpleAuthTool

echo.
echo 🎉 AUTHENTICATION COMPLETED!
echo 📊 Data service is now collecting market data
echo 🔄 Run this file again tomorrow morning.
echo.
pause 