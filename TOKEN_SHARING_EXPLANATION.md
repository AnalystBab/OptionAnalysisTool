# 🔐 HOW WINDOWS SERVICE FINDS YOUR LOGIN TOKEN

## **THE PROBLEM YOU'RE ASKING ABOUT:**
- You login manually (browser with OTP)
- Windows Service runs in background (no user interface)
- **How does the service get your login token?**

---

## **THE SOLUTION: SHARED FILE SYSTEM**

### **STEP 1: Manual Login Saves Token** 💾
When you login manually (any method), the token gets saved to:
```
C:\Users\babu\AppData\Local\OptionAnalysisTool\kite_token.json
```

**This file contains:**
```json
{
  "AccessToken": "your_actual_access_token_here",
  "ExpiryTime": "2024-12-11T00:30:00.000Z",
  "LastLoginTime": "2024-12-10T08:30:00.000Z"
}
```

### **STEP 2: Windows Service Reads Token** 🔄
The Windows Service automatically:
1. **Checks this file every 30 minutes**
2. **Loads the token** into memory
3. **Uses the token** for all API calls
4. **Collects live market data**

**Code that does this:**
```cs
// In TokenStorage.cs (line 19-23)
_tokenFile = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "OptionAnalysisTool",
    "kite_token.json"  // ← BOTH APPS ACCESS THIS FILE
);
```

---

## **MULTIPLE WAYS TO LOGIN** 🛠️

You can login using ANY of these methods, and the service will find the token:

### **Method 1: WPF Application** 
```
1. Run: OptionAnalysisTool.App.exe
2. Click: Authenticate button
3. Login: in browser with OTP
4. Token: automatically saved to shared file
5. Service: picks up token within 30 minutes
```

### **Method 2: QuickAuth Tool**
```
1. Run: dotnet run QuickAuth.cs
2. Browser: opens automatically
3. Login: with your Zerodha credentials + OTP
4. Token: captured and saved automatically
5. Service: uses the token immediately
```

### **Method 3: Manual Config Update**
```
1. Login: manually get token from Kite Connect
2. Edit: OptionAnalysisTool.Console\appsettings.json
3. Replace: "AccessToken": "your_token_here"
4. Restart: Windows Service
5. Service: reads token from config
```

### **Method 4: Environment Variable**
```
1. Set: KITE_ACCESS_TOKEN=your_token_here
2. Restart: Windows Service
3. Service: reads token from environment
```

---

## **SERVICE AUTHENTICATION PRIORITY** ⬆️

The Windows Service tries these sources in order:

1. **Shared token file** (from manual login) ← **PRIMARY**
2. **Config file** (appsettings.json)
3. **Environment variable** 
4. **Fallback:** Limited functionality mode

```cs
// From KiteAuthenticationManager.cs
public async Task<bool> InitializeAuthenticationAsync()
{
    // Try stored authentication first
    if (await LoadStoredAuthenticationAsync())
        return true;
    
    // Fallback to config
    if (await TryAuthenticateFromConfigAsync())
        return true;
    
    // No authentication available
    return false;
}
```

---

## **TOKEN EXPIRY HANDLING** ⏰

**Daily Token Lifecycle:**
- **6:00 AM IST:** Previous day's token expires
- **8:30 AM:** You need to login again (manual)
- **9:00 AM:** Service preparation starts
- **9:15 AM:** Market opens with live data

**Service Behavior:**
- **Token Valid:** Collects live data
- **Token Expired:** Runs in limited mode (no live data)
- **Token Refreshed:** Automatically detects within 30 minutes

---

## **WHAT HAPPENS WHEN TOKEN EXPIRES?** 🕐

**Before 9:15 AM (Market Open):**
```
1. Service detects expired token
2. Logs: "Session expired at 00:30 IST. Daily login required"
3. Waits: for new token from any login method
4. Continues: with test/cached data
```

**During Market Hours:**
```
1. Service detects expired token
2. Switches: to limited functionality mode
3. Logs: authentication failure messages
4. Resumes: when new token is available
```

---

## **EASY DAILY WORKFLOW** 📅

**Every Trading Day (8:30 AM):**

**Option A: Use QuickAuth Tool (Recommended)**
```bash
cd C:\Users\babu\Documents\Medha
dotnet run QuickAuth.cs
# Browser opens → Login with OTP → Token saved automatically
```

**Option B: Use WPF App**
```bash
# Run the desktop app
OptionAnalysisTool.App.exe
# Click Authenticate → Login with OTP → Token saved
```

**Option C: Manual Method**
```
1. Go to: https://kite.trade/connect/login?api_key=fgiigxn27i6ysax2&v=3
2. Login with OTP
3. Copy access token
4. Update appsettings.json
5. Restart Windows Service
```

---

## **VERIFICATION** ✅

**Check if service has valid token:**
```powershell
# Check Windows Event Viewer
Get-WinEvent -LogName Application | Where-Object {$_.ProviderName -eq "OptionMarketMonitor"} | Select-Object -First 5

# Look for messages like:
# "✅ Loaded stored authentication successfully"
# "✅ Live data available - Using stored authentication"
```

**Check token file directly:**
```powershell
Get-Content "$env:LOCALAPPDATA\OptionAnalysisTool\kite_token.json"
```

---

## **SUMMARY** 🎯

**The Magic:** File-based token sharing between manual login and Windows Service

**Your Job:** Login once daily with OTP (any method)
**Service Job:** Automatically find and use your token for 24/7 data collection

**It's like:** Leaving your car keys in a shared location - you put them there manually, the service picks them up automatically! 🔑 