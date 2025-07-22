# 🚀 SIMPLE DATA SERVICE SETUP

## **WHAT THIS DOES:**
- Collects live option market data during market hours (9:15 AM - 3:30 PM)
- Tracks circuit limit changes in real-time
- Processes end-of-day data after market close
- Runs as Windows Service (24/7 background operation)

---

## **STEP 1: GET AUTHENTICATION TOKEN** 🔑

### **Method 1: Quick Manual Method**
1. **Open this URL in browser:**
   ```
   https://kite.trade/connect/login?api_key=fgiigxn27i6ysax2&v=3
   ```

2. **Login with your Zerodha credentials**

3. **After login, you'll see a URL like:**
   ```
   http://127.0.0.1:3000/?request_token=XXXXX&action=login&status=success
   ```

4. **Copy the request_token value (the XXXXX part)**

5. **Go to this website:** https://kite.trade/docs/connect/v3/session/
   - Enter your request_token
   - Enter API Secret: `sbn6xzn6fj57hmjfitc6smpkjnux7hqw`
   - Click "Generate Session"
   - Copy the **access_token**

---

## **STEP 2: UPDATE CONFIG FILE** ⚙️

1. **Open:** `OptionAnalysisTool.Console/appsettings.json`

2. **Replace this line:**
   ```json
   "AccessToken": "your_kite_access_token_here",
   ```

3. **With your actual token:**
   ```json
   "AccessToken": "your_copied_access_token_from_step1",
   ```

4. **Save the file**

---

## **STEP 3: INSTALL WINDOWS SERVICE** 🔧

1. **Open PowerShell as Administrator**

2. **Run these commands:**
   ```powershell
   cd "C:\Users\babu\Documents\Medha\OptionAnalysisTool.Console"
   
   sc create OptionMarketMonitor binpath="C:\Users\babu\Documents\Medha\OptionAnalysisTool.Console\bin\Release\Published\OptionAnalysisTool.Console.exe" start=auto
   ```

---

## **STEP 4: START THE SERVICE** ▶️

1. **Start the service:**
   ```powershell
   sc start OptionMarketMonitor
   ```

2. **Check if it's running:**
   ```powershell
   sc query OptionMarketMonitor
   ```

---

## **STEP 5: VERIFY IT'S WORKING** ✅

1. **Check Windows Event Logs:**
   - Open Event Viewer
   - Go to Windows Logs → Application
   - Look for "OptionMarketMonitor" entries

2. **Check your database:**
   - The service saves data to: `PalindromeResults` database
   - Tables: `CircuitLimits`, `IntradaySnapshots`, etc.

---

## **DAILY TOKEN REFRESH** 🔄

**IMPORTANT:** Kite Connect tokens expire daily at 6:00 AM IST.

**Every morning before 9:00 AM:**
1. Repeat STEP 1 to get new token
2. Update appsettings.json with new token
3. Restart the service:
   ```powershell
   sc stop OptionMarketMonitor
   sc start OptionMarketMonitor
   ```

---

## **TROUBLESHOOTING** 🔧

### **Service won't start:**
```powershell
# Check service status
sc query OptionMarketMonitor

# View service logs
Get-WinEvent -LogName Application | Where-Object {$_.ProviderName -eq "OptionMarketMonitor"} | Select-Object -First 10
```

### **No data being collected:**
1. Check if token is valid (not expired)
2. Check if market is open (9:15 AM - 3:30 PM IST)
3. Check database connection string

### **Database issues:**
- Make sure SQL Server is running
- Database `PalindromeResults` exists
- Connection string is correct in appsettings.json

---

## **WHAT TO EXPECT** 📊

**During Market Hours (9:15 AM - 3:30 PM):**
- Service collects data every 30 seconds
- Circuit limits tracked in real-time
- All index options monitored (NIFTY, BANKNIFTY, FINNIFTY, etc.)

**After Market Close:**
- EOD data processing starts
- Historical data consolidated
- Database updated with daily summaries

**Non-Market Hours:**
- Service stays running
- Minimal activity (just monitoring)
- Ready for next market session

---

## **FILES TO KNOW** 📁

- **Service Executable:** `OptionAnalysisTool.Console\bin\Release\Published\OptionAnalysisTool.Console.exe`
- **Configuration:** `OptionAnalysisTool.Console\appsettings.json`
- **Database:** SQL Server, Database: `PalindromeResults`
- **Logs:** Windows Event Viewer → Application

---

**🎯 THAT'S IT! Your data service should now be running 24/7!** 