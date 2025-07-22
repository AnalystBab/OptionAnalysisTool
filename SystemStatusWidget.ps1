# Desktop System Status Widget for Indian Option Analysis Tool
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

# Configuration
$script:WidgetWidth = 350
$script:WidgetHeight = 600
$script:DatabaseName = "PalindromeResults"
$script:UpdateInterval = 5000 # 5 seconds

# Create main form
$form = New-Object System.Windows.Forms.Form
$form.Text = "Option Analysis Status"
$form.Size = New-Object System.Drawing.Size($script:WidgetWidth, $script:WidgetHeight)
$form.StartPosition = "Manual"
$form.Location = New-Object System.Drawing.Point(1570, 50) # Right side of screen
$form.FormBorderStyle = "FixedSingle"
$form.MaximizeBox = $false
$form.MinimizeBox = $true
$form.TopMost = $true
$form.BackColor = [System.Drawing.Color]::Black
$form.ForeColor = [System.Drawing.Color]::Lime

# Header Label
$headerLabel = New-Object System.Windows.Forms.Label
$headerLabel.Location = New-Object System.Drawing.Point(10, 10)
$headerLabel.Size = New-Object System.Drawing.Size(320, 30)
$headerLabel.Text = "🔥 INDIAN OPTION ANALYSIS"
$headerLabel.Font = New-Object System.Drawing.Font("Consolas", 12, [System.Drawing.FontStyle]::Bold)
$headerLabel.ForeColor = [System.Drawing.Color]::Yellow
$headerLabel.TextAlign = "MiddleCenter"
$form.Controls.Add($headerLabel)

# Market Status
$marketStatusLabel = New-Object System.Windows.Forms.Label
$marketStatusLabel.Location = New-Object System.Drawing.Point(10, 50)
$marketStatusLabel.Size = New-Object System.Drawing.Size(320, 25)
$marketStatusLabel.Font = New-Object System.Drawing.Font("Consolas", 10, [System.Drawing.FontStyle]::Bold)
$marketStatusLabel.TextAlign = "MiddleCenter"
$form.Controls.Add($marketStatusLabel)

# Authentication Status
$authLabel = New-Object System.Windows.Forms.Label
$authLabel.Location = New-Object System.Drawing.Point(10, 85)
$authLabel.Size = New-Object System.Drawing.Size(320, 40)
$authLabel.Font = New-Object System.Drawing.Font("Consolas", 9)
$form.Controls.Add($authLabel)

# Database Stats
$dbStatsLabel = New-Object System.Windows.Forms.Label
$dbStatsLabel.Location = New-Object System.Drawing.Point(10, 135)
$dbStatsLabel.Size = New-Object System.Drawing.Size(320, 80)
$dbStatsLabel.Font = New-Object System.Drawing.Font("Consolas", 9)
$form.Controls.Add($dbStatsLabel)

# Today's Activity
$todayLabel = New-Object System.Windows.Forms.Label
$todayLabel.Location = New-Object System.Drawing.Point(10, 225)
$todayLabel.Size = New-Object System.Drawing.Size(320, 100)
$todayLabel.Font = New-Object System.Drawing.Font("Consolas", 9)
$form.Controls.Add($todayLabel)

# Circuit Limit Status
$circuitLabel = New-Object System.Windows.Forms.Label
$circuitLabel.Location = New-Object System.Drawing.Point(10, 335)
$circuitLabel.Size = New-Object System.Drawing.Size(320, 80)
$circuitLabel.Font = New-Object System.Drawing.Font("Consolas", 9)
$form.Controls.Add($circuitLabel)

# System Status
$systemLabel = New-Object System.Windows.Forms.Label
$systemLabel.Location = New-Object System.Drawing.Point(10, 425)
$systemLabel.Size = New-Object System.Drawing.Size(320, 60)
$systemLabel.Font = New-Object System.Drawing.Font("Consolas", 9)
$form.Controls.Add($systemLabel)

# Action Buttons
$refreshButton = New-Object System.Windows.Forms.Button
$refreshButton.Location = New-Object System.Drawing.Point(10, 495)
$refreshButton.Size = New-Object System.Drawing.Size(100, 30)
$refreshButton.Text = "📊 Refresh"
$refreshButton.BackColor = [System.Drawing.Color]::DarkGreen
$refreshButton.ForeColor = [System.Drawing.Color]::White
$form.Controls.Add($refreshButton)

$authButton = New-Object System.Windows.Forms.Button
$authButton.Location = New-Object System.Drawing.Point(120, 495)
$authButton.Size = New-Object System.Drawing.Size(100, 30)
$authButton.Text = "🔐 Auth"
$authButton.BackColor = [System.Drawing.Color]::DarkBlue
$authButton.ForeColor = [System.Drawing.Color]::White
$form.Controls.Add($authButton)

$startButton = New-Object System.Windows.Forms.Button
$startButton.Location = New-Object System.Drawing.Point(230, 495)
$startButton.Size = New-Object System.Drawing.Size(100, 30)
$startButton.Text = "🚀 Start"
$startButton.BackColor = [System.Drawing.Color]::DarkOrange
$startButton.ForeColor = [System.Drawing.Color]::White
$form.Controls.Add($startButton)

# Update Function
function Update-Status {
    try {
        $currentTime = Get-Date
        $marketOpen = $currentTime.TimeOfDay -ge [TimeSpan]"09:15:00" -and $currentTime.TimeOfDay -le [TimeSpan]"15:30:00"
        $preMarket = $currentTime.TimeOfDay -ge [TimeSpan]"09:00:00" -and $currentTime.TimeOfDay -lt [TimeSpan]"09:15:00"
        
        # Market Status
        if ($marketOpen) {
            $marketStatusLabel.Text = "📈 MARKET OPEN"
            $marketStatusLabel.ForeColor = [System.Drawing.Color]::Lime
        } elseif ($preMarket) {
            $marketStatusLabel.Text = "⏰ PRE-MARKET"
            $marketStatusLabel.ForeColor = [System.Drawing.Color]::Yellow
        } else {
            $marketStatusLabel.Text = "🔴 MARKET CLOSED"
            $marketStatusLabel.ForeColor = [System.Drawing.Color]::Red
        }
        
        # Database Connection Test
        $connectionString = "Server=.;Database=$script:DatabaseName;Trusted_Connection=True;TrustServerCertificate=True;"
        $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
        
        try {
            $connection.Open()
            
            # Authentication Status
            $authQuery = "SELECT COUNT(*) FROM AuthenticationTokens WHERE IsActive = 1 AND ExpiryTime > GETDATE()"
            $authCmd = New-Object System.Data.SqlClient.SqlCommand($authQuery, $connection)
            $activeTokens = $authCmd.ExecuteScalar()
            
            if ($activeTokens -gt 0) {
                $authLabel.Text = "🔐 AUTH: ✅ ACTIVE`n📅 Token Valid"
                $authLabel.ForeColor = [System.Drawing.Color]::Lime
            } else {
                $authLabel.Text = "🔐 AUTH: ❌ EXPIRED`n⚠️ Run DailyAuth.bat"
                $authLabel.ForeColor = [System.Drawing.Color]::Red
            }
            
            # Today's Data
            $todayQuery = @"
                SELECT 
                    COUNT(*) as TodaySnapshots,
                    (SELECT COUNT(*) FROM CircuitLimitTrackers WHERE CAST(DetectedAt AS DATE) = CAST(GETDATE() AS DATE)) as TodayCircuits
                FROM IntradayOptionSnapshots 
                WHERE CAST(CaptureTime AS DATE) = CAST(GETDATE() AS DATE)
"@
            $todayCmd = New-Object System.Data.SqlClient.SqlCommand($todayQuery, $connection)
            $reader = $todayCmd.ExecuteReader()
            if ($reader.Read()) {
                $snapshots = $reader["TodaySnapshots"]
                $circuits = $reader["TodayCircuits"]
                $todayLabel.Text = "📊 TODAY'S DATA:`n📈 Snapshots: $snapshots`n🎯 Circuit Changes: $circuits`n⏰ Last Update: $($currentTime.ToString('HH:mm:ss'))"
                $todayLabel.ForeColor = [System.Drawing.Color]::Cyan
            }
            $reader.Close()
            
            # Database Stats
            $statsQuery = @"
                SELECT 
                    (SELECT COUNT(*) FROM IntradayOptionSnapshots) as TotalSnapshots,
                    (SELECT COUNT(*) FROM CircuitLimitTrackers) as TotalCircuits,
                    (SELECT MAX(CaptureTime) FROM IntradayOptionSnapshots) as LastSnapshot
"@
            $statsCmd = New-Object System.Data.SqlClient.SqlCommand($statsQuery, $connection)
            $statsReader = $statsCmd.ExecuteReader()
            if ($statsReader.Read()) {
                $totalSnaps = $statsReader["TotalSnapshots"]
                $totalCircuits = $statsReader["TotalCircuits"]
                $lastSnap = if ($statsReader["LastSnapshot"] -ne [DBNull]::Value) { $statsReader["LastSnapshot"] } else { "No data" }
                $dbStatsLabel.Text = "💾 DATABASE:`n📊 Total Snapshots: $totalSnaps`n🎯 Total Circuits: $totalCircuits`n🕐 Last Activity:`n   $lastSnap"
                $dbStatsLabel.ForeColor = [System.Drawing.Color]::White
            }
            $statsReader.Close()
            
            # System Status
            $dotnetProcesses = @(Get-Process | Where-Object {$_.ProcessName -like "*dotnet*"}).Count
            $systemLabel.Text = "🖥️ SYSTEM:`n✅ Database: Connected`n🔄 Processes: $dotnetProcesses .NET apps`n📡 Monitoring: Active"
            $systemLabel.ForeColor = [System.Drawing.Color]::Lime
            
        } catch {
            $authLabel.Text = "🔐 AUTH: ❓ UNKNOWN`n❌ Database Error"
            $authLabel.ForeColor = [System.Drawing.Color]::Orange
            $dbStatsLabel.Text = "💾 DATABASE:`n❌ Connection Failed`n🔧 Check SQL Server"
            $dbStatsLabel.ForeColor = [System.Drawing.Color]::Red
            $systemLabel.Text = "🖥️ SYSTEM:`n❌ Database: Error`n🔧 Check Connection"
            $systemLabel.ForeColor = [System.Drawing.Color]::Red
        } finally {
            $connection.Close()
        }
        
    } catch {
        # Handle any other errors
        $systemLabel.Text = "🖥️ SYSTEM:`n❌ Widget Error`n🔧 Check Configuration"
        $systemLabel.ForeColor = [System.Drawing.Color]::Red
    }
}

# Timer for auto-refresh
$timer = New-Object System.Windows.Forms.Timer
$timer.Interval = $script:UpdateInterval
$timer.Add_Tick({ Update-Status })
$timer.Start()

# Button Events
$refreshButton.Add_Click({ Update-Status })
$authButton.Add_Click({ 
    Start-Process -FilePath ".\DailyAuth.bat" -WorkingDirectory (Get-Location)
})
$startButton.Add_Click({
    Start-Process -FilePath ".\StartLiveMonitoring.bat" -WorkingDirectory (Get-Location)
})

# Initial update
Update-Status

# Show form
$form.ShowDialog() | Out-Null 