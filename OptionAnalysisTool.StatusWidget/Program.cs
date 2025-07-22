using System;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;

namespace OptionAnalysisTool.StatusWidget
{
    /// <summary>
    /// 🖥️ DESKTOP STATUS WIDGET - SYSTEM TRAY APPLICATION
    /// 
    /// REAL-TIME MONITORING:
    /// ✅ Authentication status and token expiry
    /// ✅ Data collection service health
    /// ✅ Database connectivity
    /// ✅ Market hours and data flow
    /// ✅ Circuit limit monitoring status
    /// ✅ Error alerts and notifications
    /// 
    /// INTERACTIVE FEATURES:
    /// 🔐 Quick re-authentication when needed
    /// 📊 Service status dashboard
    /// 🔔 Desktop notifications for issues
    /// ⚙️ Service control (start/stop/restart)
    /// 📈 Real-time data statistics
    /// </summary>
    public partial class StatusWidget : Form
    {
        private NotifyIcon _trayIcon;
        private ContextMenuStrip _contextMenu;
        private Timer _statusTimer;
        private ServiceStatus _lastStatus;
        private DateTime _lastNotification = DateTime.MinValue;
        
        // SECURE TOKEN STORAGE
        private readonly string _tokenStoragePath;
        private readonly string _statusCachePath;
        
        // UI COMPONENTS
        private ToolStripMenuItem _authStatusItem;
        private ToolStripMenuItem _dataServiceItem;
        private ToolStripMenuItem _databaseItem;
        private ToolStripMenuItem _marketStatusItem;
        private ToolStripMenuItem _lastUpdateItem;

        public StatusWidget()
        {
            // SETUP SECURE STORAGE
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "OptionAnalysisTool");
            Directory.CreateDirectory(appDataPath);
            
            _tokenStoragePath = Path.Combine(appDataPath, "secure_tokens.dat");
            _statusCachePath = Path.Combine(appDataPath, "service_status.json");
            
            InitializeComponent();
            SetupSystemTray();
            StartStatusMonitoring();
        }

        /// <summary>
        /// SETUP SYSTEM TRAY ICON AND MENU
        /// </summary>
        private void SetupSystemTray()
        {
            // CREATE CONTEXT MENU
            _contextMenu = new ContextMenuStrip();
            
            // STATUS SECTION
            _contextMenu.Items.Add("📊 OPTION DATA SERVICE STATUS", null, null).Enabled = false;
            _contextMenu.Items.Add(new ToolStripSeparator());
            
            _authStatusItem = new ToolStripMenuItem("🔐 Authentication: Checking...");
            _dataServiceItem = new ToolStripMenuItem("📈 Data Service: Checking...");
            _databaseItem = new ToolStripMenuItem("💾 Database: Checking...");
            _marketStatusItem = new ToolStripMenuItem("🕐 Market: Checking...");
            _lastUpdateItem = new ToolStripMenuItem("⏰ Last Update: Never");
            
            _contextMenu.Items.AddRange(new ToolStripItem[]
            {
                _authStatusItem,
                _dataServiceItem, 
                _databaseItem,
                _marketStatusItem,
                _lastUpdateItem
            });
            
            _contextMenu.Items.Add(new ToolStripSeparator());
            
            // ACTION SECTION
            _contextMenu.Items.Add("🔄 Actions", null, null).Enabled = false;
            _contextMenu.Items.Add(new ToolStripSeparator());
            
            var authenticateItem = new ToolStripMenuItem("🔐 Authenticate Now", null, OnAuthenticateClick);
            var dashboardItem = new ToolStripMenuItem("📊 Open Dashboard", null, OnDashboardClick);
            var dataViewItem = new ToolStripMenuItem("📈 View Data", null, OnDataViewClick);
            var logsItem = new ToolStripMenuItem("📝 View Logs", null, OnLogsClick);
            
            _contextMenu.Items.AddRange(new ToolStripItem[]
            {
                authenticateItem,
                dashboardItem,
                dataViewItem,
                logsItem
            });
            
            _contextMenu.Items.Add(new ToolStripSeparator());
            
            // SERVICE CONTROL
            _contextMenu.Items.Add("⚙️ Service Control", null, null).Enabled = false;
            _contextMenu.Items.Add(new ToolStripSeparator());
            
            var startServiceItem = new ToolStripMenuItem("▶️ Start Data Service", null, OnStartServiceClick);
            var stopServiceItem = new ToolStripMenuItem("⏸️ Stop Data Service", null, OnStopServiceClick);
            var restartServiceItem = new ToolStripMenuItem("🔄 Restart Service", null, OnRestartServiceClick);
            
            _contextMenu.Items.AddRange(new ToolStripItem[]
            {
                startServiceItem,
                stopServiceItem,
                restartServiceItem
            });
            
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add("❌ Exit", null, OnExitClick);

            // CREATE TRAY ICON
            _trayIcon = new NotifyIcon()
            {
                Icon = CreateStatusIcon(ServiceStatusType.Unknown),
                ContextMenuStrip = _contextMenu,
                Visible = true,
                Text = "Option Data Service - Status Unknown"
            };
            
            _trayIcon.DoubleClick += OnTrayDoubleClick;
            _trayIcon.BalloonTipClicked += OnBalloonTipClicked;
        }

        /// <summary>
        /// START STATUS MONITORING TIMER
        /// </summary>
        private void StartStatusMonitoring()
        {
            _statusTimer = new Timer();
            _statusTimer.Interval = 30000; // Check every 30 seconds
            _statusTimer.Tick += async (s, e) => await UpdateServiceStatusAsync();
            _statusTimer.Start();
            
            // Initial status check
            Task.Run(async () => await UpdateServiceStatusAsync());
        }

        /// <summary>
        /// UPDATE SERVICE STATUS - MAIN MONITORING FUNCTION
        /// </summary>
        private async Task UpdateServiceStatusAsync()
        {
            try
            {
                var status = await GetComprehensiveServiceStatusAsync();
                
                // UPDATE UI
                Invoke(() => UpdateTrayIconAndMenu(status));
                
                // CHECK FOR CRITICAL ISSUES
                await HandleCriticalIssuesAsync(status);
                
                // CACHE STATUS
                await CacheServiceStatusAsync(status);
                
                _lastStatus = status;
            }
            catch (Exception ex)
            {
                // ERROR IN STATUS CHECK
                var errorStatus = new ServiceStatus
                {
                    OverallHealth = ServiceStatusType.Error,
                    ErrorMessage = ex.Message,
                    LastChecked = DateTime.Now
                };
                
                Invoke(() => UpdateTrayIconAndMenu(errorStatus));
            }
        }

        /// <summary>
        /// GET COMPREHENSIVE SERVICE STATUS
        /// </summary>
        private async Task<ServiceStatus> GetComprehensiveServiceStatusAsync()
        {
            var status = new ServiceStatus
            {
                LastChecked = DateTime.Now
            };

            try
            {
                // CHECK AUTHENTICATION STATUS
                status.AuthenticationStatus = await CheckAuthenticationStatusAsync();
                
                // CHECK DATA SERVICE STATUS
                status.DataServiceStatus = await CheckDataServiceStatusAsync();
                
                // CHECK DATABASE STATUS
                status.DatabaseStatus = await CheckDatabaseStatusAsync();
                
                // CHECK MARKET STATUS
                status.MarketStatus = await CheckMarketStatusAsync();
                
                // CHECK LAST DATA COLLECTION
                status.LastDataCollection = await GetLastDataCollectionTimeAsync();
                
                // CHECK CIRCUIT LIMIT MONITORING
                status.CircuitLimitStatus = await CheckCircuitLimitMonitoringAsync();
                
                // CALCULATE OVERALL HEALTH
                status.OverallHealth = CalculateOverallHealth(status);
                
                return status;
            }
            catch (Exception ex)
            {
                status.OverallHealth = ServiceStatusType.Error;
                status.ErrorMessage = ex.Message;
                return status;
            }
        }

        /// <summary>
        /// CHECK AUTHENTICATION STATUS AND TOKEN VALIDITY
        /// </summary>
        private async Task<AuthStatus> CheckAuthenticationStatusAsync()
        {
            try
            {
                var tokenData = await LoadSecureTokenAsync();
                
                if (tokenData == null)
                {
                    return new AuthStatus
                    {
                        IsAuthenticated = false,
                        Status = ServiceStatusType.Warning,
                        Message = "No authentication token found",
                        TokenExpiry = null
                    };
                }
                
                var timeUntilExpiry = tokenData.ExpiresAt - DateTime.UtcNow;
                
                if (timeUntilExpiry.TotalMinutes < 0)
                {
                    return new AuthStatus
                    {
                        IsAuthenticated = false,
                        Status = ServiceStatusType.Error,
                        Message = "Token expired",
                        TokenExpiry = tokenData.ExpiresAt
                    };
                }
                else if (timeUntilExpiry.TotalHours < 1)
                {
                    return new AuthStatus
                    {
                        IsAuthenticated = true,
                        Status = ServiceStatusType.Warning,
                        Message = $"Token expires in {timeUntilExpiry.TotalMinutes:F0} minutes",
                        TokenExpiry = tokenData.ExpiresAt
                    };
                }
                else
                {
                    return new AuthStatus
                    {
                        IsAuthenticated = true,
                        Status = ServiceStatusType.Healthy,
                        Message = $"Token valid for {timeUntilExpiry.TotalHours:F1} hours",
                        TokenExpiry = tokenData.ExpiresAt
                    };
                }
            }
            catch (Exception ex)
            {
                return new AuthStatus
                {
                    IsAuthenticated = false,
                    Status = ServiceStatusType.Error,
                    Message = $"Auth check failed: {ex.Message}",
                    TokenExpiry = null
                };
            }
        }

        /// <summary>
        /// CHECK DATA SERVICE STATUS
        /// </summary>
        private async Task<DataServiceStatus> CheckDataServiceStatusAsync()
        {
            try
            {
                // CHECK IF DATA COLLECTION SERVICE IS RUNNING
                var processes = Process.GetProcessesByName("OptionAnalysisTool.Console");
                
                if (processes.Length == 0)
                {
                    return new DataServiceStatus
                    {
                        IsRunning = false,
                        Status = ServiceStatusType.Error,
                        Message = "Data service not running"
                    };
                }
                
                // CHECK RECENT DATA COLLECTION
                var lastCollection = await GetLastDataCollectionTimeAsync();
                var timeSinceLastCollection = DateTime.Now - lastCollection;
                
                if (timeSinceLastCollection.TotalMinutes > 5 && IsMarketHours())
                {
                    return new DataServiceStatus
                    {
                        IsRunning = true,
                        Status = ServiceStatusType.Warning,
                        Message = $"No data collected for {timeSinceLastCollection.TotalMinutes:F0} minutes"
                    };
                }
                
                return new DataServiceStatus
                {
                    IsRunning = true,
                    Status = ServiceStatusType.Healthy,
                    Message = $"Active - Last collection: {timeSinceLastCollection.TotalMinutes:F0}m ago"
                };
            }
            catch (Exception ex)
            {
                return new DataServiceStatus
                {
                    IsRunning = false,
                    Status = ServiceStatusType.Error,
                    Message = $"Service check failed: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// UPDATE TRAY ICON AND MENU WITH STATUS
        /// </summary>
        private void UpdateTrayIconAndMenu(ServiceStatus status)
        {
            // UPDATE TRAY ICON
            _trayIcon.Icon = CreateStatusIcon(status.OverallHealth);
            _trayIcon.Text = GetStatusText(status);
            
            // UPDATE MENU ITEMS
            _authStatusItem.Text = $"🔐 Auth: {GetAuthStatusText(status.AuthenticationStatus)}";
            _authStatusItem.Image = GetStatusImage(status.AuthenticationStatus.Status);
            
            _dataServiceItem.Text = $"📈 Service: {GetDataServiceStatusText(status.DataServiceStatus)}";
            _dataServiceItem.Image = GetStatusImage(status.DataServiceStatus.Status);
            
            _databaseItem.Text = $"💾 Database: {GetDatabaseStatusText(status.DatabaseStatus)}";
            _databaseItem.Image = GetStatusImage(status.DatabaseStatus);
            
            _marketStatusItem.Text = $"🕐 Market: {status.MarketStatus}";
            
            _lastUpdateItem.Text = $"⏰ Updated: {status.LastChecked:HH:mm:ss}";
        }

        /// <summary>
        /// HANDLE CRITICAL ISSUES AND NOTIFICATIONS
        /// </summary>
        private async Task HandleCriticalIssuesAsync(ServiceStatus status)
        {
            var now = DateTime.Now;
            
            // AVOID SPAM NOTIFICATIONS
            if (now - _lastNotification < TimeSpan.FromMinutes(10))
                return;

            // AUTHENTICATION REQUIRED
            if (!status.AuthenticationStatus.IsAuthenticated && IsMarketHours())
            {
                ShowNotification(
                    "🔐 Authentication Required",
                    "Data service needs authentication to continue collecting data.",
                    ToolTipIcon.Warning,
                    NotificationAction.Authenticate
                );
                _lastNotification = now;
                return;
            }

            // TOKEN EXPIRING SOON
            if (status.AuthenticationStatus.TokenExpiry.HasValue)
            {
                var timeUntilExpiry = status.AuthenticationStatus.TokenExpiry.Value - DateTime.UtcNow;
                if (timeUntilExpiry.TotalMinutes < 30 && timeUntilExpiry.TotalMinutes > 0 && IsMarketHours())
                {
                    ShowNotification(
                        "⏰ Token Expiring Soon",
                        $"Access token expires in {timeUntilExpiry.TotalMinutes:F0} minutes. Please re-authenticate.",
                        ToolTipIcon.Warning,
                        NotificationAction.Authenticate
                    );
                    _lastNotification = now;
                    return;
                }
            }

            // DATA SERVICE DOWN
            if (!status.DataServiceStatus.IsRunning && IsMarketHours())
            {
                ShowNotification(
                    "🚨 Data Service Down",
                    "Option data collection service is not running during market hours!",
                    ToolTipIcon.Error,
                    NotificationAction.StartService
                );
                _lastNotification = now;
                return;
            }

            // NO DATA COLLECTION
            var timeSinceCollection = now - status.LastDataCollection;
            if (timeSinceCollection.TotalMinutes > 10 && IsMarketHours())
            {
                ShowNotification(
                    "📊 Data Collection Issue",
                    $"No data collected for {timeSinceCollection.TotalMinutes:F0} minutes during market hours.",
                    ToolTipIcon.Warning,
                    NotificationAction.CheckService
                );
                _lastNotification = now;
            }
        }

        /// <summary>
        /// SHOW DESKTOP NOTIFICATION
        /// </summary>
        private void ShowNotification(string title, string message, ToolTipIcon icon, NotificationAction action)
        {
            _trayIcon.BalloonTipTitle = title;
            _trayIcon.BalloonTipText = message;
            _trayIcon.BalloonTipIcon = icon;
            _trayIcon.Tag = action; // Store action for click handling
            _trayIcon.ShowBalloonTip(5000);
        }

        /// <summary>
        /// EVENT HANDLERS
        /// </summary>
        private async void OnAuthenticateClick(object sender, EventArgs e)
        {
            await LaunchAuthenticationAsync();
        }

        private void OnDashboardClick(object sender, EventArgs e)
        {
            LaunchDashboard();
        }

        private void OnDataViewClick(object sender, EventArgs e)
        {
            LaunchDataViewer();
        }

        private void OnLogsClick(object sender, EventArgs e)
        {
            LaunchLogViewer();
        }

        private async void OnStartServiceClick(object sender, EventArgs e)
        {
            await StartDataServiceAsync();
        }

        private async void OnStopServiceClick(object sender, EventArgs e)
        {
            await StopDataServiceAsync();
        }

        private async void OnRestartServiceClick(object sender, EventArgs e)
        {
            await RestartDataServiceAsync();
        }

        private void OnTrayDoubleClick(object sender, EventArgs e)
        {
            LaunchDashboard();
        }

        private async void OnBalloonTipClicked(object sender, EventArgs e)
        {
            if (_trayIcon.Tag is NotificationAction action)
            {
                switch (action)
                {
                    case NotificationAction.Authenticate:
                        await LaunchAuthenticationAsync();
                        break;
                    case NotificationAction.StartService:
                        await StartDataServiceAsync();
                        break;
                    case NotificationAction.CheckService:
                        LaunchDashboard();
                        break;
                }
            }
        }

        private void OnExitClick(object sender, EventArgs e)
        {
            _trayIcon.Visible = false;
            Application.Exit();
        }

        /// <summary>
        /// LAUNCH AUTHENTICATION PROCESS
        /// </summary>
        private async Task LaunchAuthenticationAsync()
        {
            try
            {
                var authProcess = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "run --project DailyAuth.csproj",
                    WorkingDirectory = Application.StartupPath,
                    UseShellExecute = true
                };
                
                Process.Start(authProcess);
                
                ShowNotification(
                    "🔐 Authentication Started",
                    "Authentication process launched. Please complete login with OTP.",
                    ToolTipIcon.Info,
                    NotificationAction.None
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to launch authentication: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// SECURE TOKEN MANAGEMENT
        /// </summary>
        private async Task<SecureToken?> LoadSecureTokenAsync()
        {
            try
            {
                if (!File.Exists(_tokenStoragePath))
                    return null;

                var encryptedData = await File.ReadAllBytesAsync(_tokenStoragePath);
                var json = UnprotectData(encryptedData);
                
                return JsonSerializer.Deserialize<SecureToken>(json);
            }
            catch
            {
                return null;
            }
        }

        private async Task StoreSecureTokenAsync(SecureToken token)
        {
            var json = JsonSerializer.Serialize(token);
            var encryptedData = ProtectData(json);
            await File.WriteAllBytesAsync(_tokenStoragePath, encryptedData);
        }

        /// <summary>
        /// ENCRYPTION/DECRYPTION UTILITIES
        /// </summary>
        private byte[] ProtectData(string data)
        {
            var bytes = Encoding.UTF8.GetBytes(data);
            return ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        }

        private string UnprotectData(byte[] encryptedData)
        {
            var bytes = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// UTILITY METHODS
        /// </summary>
        private Icon CreateStatusIcon(ServiceStatusType status)
        {
            var bitmap = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bitmap))
            {
                var color = status switch
                {
                    ServiceStatusType.Healthy => Color.Green,
                    ServiceStatusType.Warning => Color.Orange,
                    ServiceStatusType.Error => Color.Red,
                    _ => Color.Gray
                };
                
                g.FillEllipse(new SolidBrush(color), 2, 2, 12, 12);
            }
            
            return Icon.FromHandle(bitmap.GetHicon());
        }

        private bool IsMarketHours()
        {
            var now = DateTime.Now;
            var startTime = new TimeSpan(9, 15, 0);  // 9:15 AM
            var endTime = new TimeSpan(15, 30, 0);   // 3:30 PM
            
            return now.DayOfWeek != DayOfWeek.Saturday && 
                   now.DayOfWeek != DayOfWeek.Sunday &&
                   now.TimeOfDay >= startTime && 
                   now.TimeOfDay <= endTime;
        }

        // Additional helper methods for status checking, data retrieval, etc.
        // ... (implementation continues)

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _trayIcon?.Dispose();
                _statusTimer?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Visible = false;
        }
    }

    /// <summary>
    /// DATA MODELS FOR STATUS TRACKING
    /// </summary>
    public class ServiceStatus
    {
        public DateTime LastChecked { get; set; }
        public ServiceStatusType OverallHealth { get; set; }
        public AuthStatus AuthenticationStatus { get; set; } = new();
        public DataServiceStatus DataServiceStatus { get; set; } = new();
        public ServiceStatusType DatabaseStatus { get; set; }
        public string MarketStatus { get; set; } = "";
        public DateTime LastDataCollection { get; set; }
        public ServiceStatusType CircuitLimitStatus { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class AuthStatus
    {
        public bool IsAuthenticated { get; set; }
        public ServiceStatusType Status { get; set; }
        public string Message { get; set; } = "";
        public DateTime? TokenExpiry { get; set; }
    }

    public class DataServiceStatus
    {
        public bool IsRunning { get; set; }
        public ServiceStatusType Status { get; set; }
        public string Message { get; set; } = "";
    }

    public class SecureToken
    {
        public required string AccessToken { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public required string UserId { get; set; }
    }

    public enum ServiceStatusType
    {
        Unknown,
        Healthy,
        Warning,
        Error
    }

    public enum NotificationAction
    {
        None,
        Authenticate,
        StartService,
        CheckService
    }

    /// <summary>
    /// APPLICATION ENTRY POINT
    /// </summary>
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // ENSURE SINGLE INSTANCE
            bool createdNew;
            using (var mutex = new System.Threading.Mutex(true, "OptionAnalysisStatusWidget", out createdNew))
            {
                if (createdNew)
                {
                    Application.Run(new StatusWidget());
                }
                else
                {
                    MessageBox.Show("Status widget is already running!", "Already Running", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }
} 