# 🚀 QUICK START - GET DATA SERVICE WORKING NOW!

## **WHAT YOU NEED TO DO:**

### **STEP 1: Get Kite Token** 🔑
1. **Open this link:** https://kite.trade/connect/login?api_key=fgiigxn27i6ysax2&v=3
2. **Login** with your Zerodha credentials
3. **Copy the request_token** from the redirect URL (looks like: `request_token=XXXXX`)
4. **Go here:** https://sessiontoken.kite.trade/
5. **Enter:**
   - Request Token: (the token you copied)
   - API Secret: `sbn6xzn6fj57hmjfitc6smpkjnux7hqw`
6. **Click Generate** and **copy the access_token**

### **STEP 2: Update Config** ⚙️
1. **Open:** `OptionAnalysisTool.Console\appsettings.json`
2. **Find this line:**
   ```json
   "AccessToken": "your_kite_access_token_here",
   ```
3. **Replace with your token:**
   ```json
   "AccessToken": "the_access_token_you_copied",
   ```
4. **Save the file**

### **STEP 3: Install Service** 🔧
**Open PowerShell as Administrator and run:**
```powershell
cd "C:\Users\babu\Documents\Medha\OptionAnalysisTool.Console"

sc create OptionMarketMonitor binpath="C:\Users\babu\Documents\Medha\OptionAnalysisTool.Console\bin\Release\Published\OptionAnalysisTool.Console.exe" start=auto

sc start OptionMarketMonitor
```

### **STEP 4: Check if Working** ✅
```powershell
sc query OptionMarketMonitor
```

**You should see:** `STATE: 4 RUNNING`

---

## **THAT'S IT! 🎉**

**Your data service is now:**
- ✅ Running 24/7 as Windows Service
- ✅ Collecting live market data (9:15 AM - 3:30 PM)
- ✅ Tracking circuit limits in real-time  
- ✅ Saving data to SQL Server database `PalindromeResults`

---

## **DAILY MAINTENANCE** 🔄
**Every morning before 9 AM:**
1. Repeat STEP 1 to get new token
2. Update appsettings.json with new token  
3. Restart service:
   ```powershell
   sc stop OptionMarketMonitor
   sc start OptionMarketMonitor
   ```

---

## **TROUBLESHOOTING** 🛠️

**Service won't start?**
- Check Event Viewer → Windows Logs → Application
- Look for "OptionMarketMonitor" entries

**No data?** 
- Token expired (get new one)
- Market closed (data only collected 9:15 AM - 3:30 PM IST)

**Database issues?**
- Make sure SQL Server is running
- Database `PalindromeResults` exists

---

**🚀 SIMPLE AS THAT! Your automated option data service is ready!** 