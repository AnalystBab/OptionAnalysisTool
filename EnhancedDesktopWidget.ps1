# Enhanced Desktop Widget for Indian Option Analysis Tool
# Uses 25% of desktop space optimally (Right side, 25% width, 75% height)

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Data.SqlClient

# Get screen dimensions
$screen = [System.Windows.Forms.Screen]::PrimaryScreen.WorkingArea
$widgetWidth = [int]($screen.Width * 0.25)    # 25% of screen width
$widgetHeight = [int]($screen.Height * 0.75)  # 75% of screen height
$widgetX = $screen.Width - $widgetWidth - 10   # 10px from right edge
$widgetY = 10                                  # 10px from top

Write-Host "🖥️ Screen Resolution: $($screen.Width) x $($screen.Height)" -ForegroundColor Cyan
Write-Host "📱 Widget Size: $widgetWidth x $widgetHeight" -ForegroundColor Green
Write-Host "📍 Widget Position: X=$widgetX, Y=$widgetY" -ForegroundColor Yellow

# Main Widget Form
$widget = New-Object System.Windows.Forms.Form
$widget.Text = "Option Analysis Monitor"
$widget.Size = New-Object System.Drawing.Size($widgetWidth, $widgetHeight)
$widget.StartPosition = "Manual"
$widget.Location = New-Object System.Drawing.Point($widgetX, $widgetY)
$widget.FormBorderStyle = [System.Windows.Forms.FormBorderStyle]::FixedSingle
$widget.MaximizeBox = $false
$widget.MinimizeBox = $true
$widget.TopMost = $true
$widget.BackColor = [System.Drawing.Color]::FromArgb(15, 15, 20)
$widget.ForeColor = [System.Drawing.Color]::White

# Create scrollable panel for content
$scrollPanel = New-Object System.Windows.Forms.Panel
$scrollPanel.Location = New-Object System.Drawing.Point(0, 0)
$scrollPanel.Size = New-Object System.Drawing.Size($widgetWidth, $widgetHeight)
$scrollPanel.AutoScroll = $true
$scrollPanel.BackColor = [System.Drawing.Color]::Transparent
$widget.Controls.Add($scrollPanel)

$yPos = 10

# Header Section
$headerLabel = New-Object System.Windows.Forms.Label
$headerLabel.Location = New-Object System.Drawing.Point(10, $yPos)
$headerLabel.Size = New-Object System.Drawing.Size(($widgetWidth - 20), 35)
$headerLabel.Text = "INDIAN OPTION ANALYSIS"
$headerLabel.Font = New-Object System.Drawing.Font("Segoe UI", 14, [System.Drawing.FontStyle]::Bold)
$headerLabel.ForeColor = [System.Drawing.Color]::Gold
$headerLabel.TextAlign = "MiddleCenter"
$scrollPanel.Controls.Add($headerLabel)
$yPos += 45

# Market Status Section
$marketStatusPanel = New-Object System.Windows.Forms.Panel
$marketStatusPanel.Location = New-Object System.Drawing.Point(10, $yPos)
$marketStatusPanel.Size = New-Object System.Drawing.Size(($widgetWidth - 20), 60)
$marketStatusPanel.BackColor = [System.Drawing.Color]::FromArgb(25, 25, 35)
$marketStatusPanel.BorderStyle = [System.Windows.Forms.BorderStyle]::FixedSingle
$scrollPanel.Controls.Add($marketStatusPanel)

$marketLabel = New-Object System.Windows.Forms.Label
$marketLabel.Location = New-Object System.Drawing.Point(5, 5)
$marketLabel.Size = New-Object System.Drawing.Size(($widgetWidth - 30), 25)
$marketLabel.Font = New-Object System.Drawing.Font("Segoe UI", 12, [System.Drawing.FontStyle]::Bold)
$marketLabel.TextAlign = "MiddleCenter"
$marketStatusPanel.Controls.Add($marketLabel)

$timeLabel = New-Object System.Windows.Forms.Label
$timeLabel.Location = New-Object System.Drawing.Point(5, 30)
$timeLabel.Size = New-Object System.Drawing.Size(($widgetWidth - 30), 20)
$timeLabel.Font = New-Object System.Drawing.Font("Consolas", 10)
$timeLabel.ForeColor = [System.Drawing.Color]::LightGray
$timeLabel.TextAlign = "MiddleCenter"
$marketStatusPanel.Controls.Add($timeLabel)
$yPos += 70

# Authentication Section
$authPanel = New-Object System.Windows.Forms.Panel
$authPanel.Location = New-Object System.Drawing.Point(10, $yPos)
$authPanel.Size = New-Object System.Drawing.Size(($widgetWidth - 20), 50)
$authPanel.BackColor = [System.Drawing.Color]::FromArgb(25, 25, 35)
$authPanel.BorderStyle = [System.Windows.Forms.BorderStyle]::FixedSingle
$scrollPanel.Controls.Add($authPanel)

$authLabel = New-Object System.Windows.Forms.Label
$authLabel.Location = New-Object System.Drawing.Point(5, 5)
$authLabel.Size = New-Object System.Drawing.Size(($widgetWidth - 30), 40)
$authLabel.Font = New-Object System.Drawing.Font("Segoe UI", 10)
$authLabel.TextAlign = "MiddleLeft"
$authPanel.Controls.Add($authLabel)
$yPos += 60

# Today's Data Section
$todayPanel = New-Object System.Windows.Forms.Panel
$todayPanel.Location = New-Object System.Drawing.Point(10, $yPos)
$todayPanel.Size = New-Object System.Drawing.Size(($widgetWidth - 20), 100)
$todayPanel.BackColor = [System.Drawing.Color]::FromArgb(25, 25, 35)
$todayPanel.BorderStyle = [System.Windows.Forms.BorderStyle]::FixedSingle
$scrollPanel.Controls.Add($todayPanel)

$todayLabel = New-Object System.Windows.Forms.Label
$todayLabel.Location = New-Object System.Drawing.Point(5, 5)
$todayLabel.Size = New-Object System.Drawing.Size(($widgetWidth - 30), 90)
$todayLabel.Font = New-Object System.Drawing.Font("Consolas", 9)
$todayLabel.TextAlign = "TopLeft"
$todayPanel.Controls.Add($todayLabel)
$yPos += 110

# Database Stats Section
$dbPanel = New-Object System.Windows.Forms.Panel
$dbPanel.Location = New-Object System.Drawing.Point(10, $yPos)
$dbPanel.Size = New-Object System.Drawing.Size(($widgetWidth - 20), 120)
$dbPanel.BackColor = [System.Drawing.Color]::FromArgb(25, 25, 35)
$dbPanel.BorderStyle = [System.Windows.Forms.BorderStyle]::FixedSingle
$scrollPanel.Controls.Add($dbPanel)

$dbLabel = New-Object System.Windows.Forms.Label
$dbLabel.Location = New-Object System.Drawing.Point(5, 5)
$dbLabel.Size = New-Object System.Drawing.Size(($widgetWidth - 30), 110)
$dbLabel.Font = New-Object System.Drawing.Font("Consolas", 9)
$dbLabel.TextAlign = "TopLeft"
$dbPanel.Controls.Add($dbLabel)
$yPos += 130

# Circuit Monitoring Section
$circuitPanel = New-Object System.Windows.Forms.Panel
$circuitPanel.Location = New-Object System.Drawing.Point(10, $yPos)
$circuitPanel.Size = New-Object System.Drawing.Size(($widgetWidth - 20), 80)
$circuitPanel.BackColor = [System.Drawing.Color]::FromArgb(25, 25, 35)
$circuitPanel.BorderStyle = [System.Windows.Forms.BorderStyle]::FixedSingle
$scrollPanel.Controls.Add($circuitPanel)

$circuitLabel = New-Object System.Windows.Forms.Label
$circuitLabel.Location = New-Object System.Drawing.Point(5, 5)
$circuitLabel.Size = New-Object System.Drawing.Size(($widgetWidth - 30), 70)
$circuitLabel.Font = New-Object System.Drawing.Font("Consolas", 9)
$circuitLabel.TextAlign = "TopLeft"
$circuitPanel.Controls.Add($circuitLabel)
$yPos += 90

# System Health Section
$systemPanel = New-Object System.Windows.Forms.Panel
$systemPanel.Location = New-Object System.Drawing.Point(10, $yPos)
$systemPanel.Size = New-Object System.Drawing.Size(($widgetWidth - 20), 80)
$systemPanel.BackColor = [System.Drawing.Color]::FromArgb(25, 25, 35)
$systemPanel.BorderStyle = [System.Windows.Forms.BorderStyle]::FixedSingle
$scrollPanel.Controls.Add($systemPanel)

$systemLabel = New-Object System.Windows.Forms.Label
$systemLabel.Location = New-Object System.Drawing.Point(5, 5)
$systemLabel.Size = New-Object System.Drawing.Size(($widgetWidth - 30), 70)
$systemLabel.Font = New-Object System.Drawing.Font("Consolas", 9)
$systemLabel.TextAlign = "TopLeft"
$systemPanel.Controls.Add($systemLabel)
$yPos += 90

# Action Buttons Section
$buttonPanel = New-Object System.Windows.Forms.Panel
$buttonPanel.Location = New-Object System.Drawing.Point(10, $yPos)
$buttonPanel.Size = New-Object System.Drawing.Size(($widgetWidth - 20), 120)
$buttonPanel.BackColor = [System.Drawing.Color]::FromArgb(25, 25, 35)
$buttonPanel.BorderStyle = [System.Windows.Forms.BorderStyle]::FixedSingle
$scrollPanel.Controls.Add($buttonPanel)

$buttonWidth = [int](($widgetWidth - 40) / 2)
$buttonHeight = 35

# Auth Button
$authButton = New-Object System.Windows.Forms.Button
$authButton.Location = New-Object System.Drawing.Point(5, 10)
$authButton.Size = New-Object System.Drawing.Size($buttonWidth, $buttonHeight)
$authButton.Text = "Auth"
$authButton.BackColor = [System.Drawing.Color]::FromArgb(70, 130, 180)
$authButton.ForeColor = [System.Drawing.Color]::White
$authButton.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
$authButton.Font = New-Object System.Drawing.Font("Segoe UI", 9)
$buttonPanel.Controls.Add($authButton)

# Start Button
$startButton = New-Object System.Windows.Forms.Button
$startButton.Location = New-Object System.Drawing.Point((10 + $buttonWidth), 10)
$startButton.Size = New-Object System.Drawing.Size($buttonWidth, $buttonHeight)
$startButton.Text = "Start"
$startButton.BackColor = [System.Drawing.Color]::FromArgb(34, 139, 34)
$startButton.ForeColor = [System.Drawing.Color]::White
$startButton.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
$startButton.Font = New-Object System.Drawing.Font("Segoe UI", 9)
$buttonPanel.Controls.Add($startButton)

# Refresh Button
$refreshButton = New-Object System.Windows.Forms.Button
$refreshButton.Location = New-Object System.Drawing.Point(5, 50)
$refreshButton.Size = New-Object System.Drawing.Size($buttonWidth, $buttonHeight)
$refreshButton.Text = "Refresh"
$refreshButton.BackColor = [System.Drawing.Color]::FromArgb(255, 140, 0)
$refreshButton.ForeColor = [System.Drawing.Color]::White
$refreshButton.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
$refreshButton.Font = New-Object System.Drawing.Font("Segoe UI", 9)
$buttonPanel.Controls.Add($refreshButton)

# Help Button
$helpButton = New-Object System.Windows.Forms.Button
$helpButton.Location = New-Object System.Drawing.Point((10 + $buttonWidth), 50)
$helpButton.Size = New-Object System.Drawing.Size($buttonWidth, $buttonHeight)
$helpButton.Text = "Help"
$helpButton.BackColor = [System.Drawing.Color]::FromArgb(147, 112, 219)
$helpButton.ForeColor = [System.Drawing.Color]::White
$helpButton.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
$helpButton.Font = New-Object System.Drawing.Font("Segoe UI", 9)
$buttonPanel.Controls.Add($helpButton)

$yPos += 130

# Functions
function Update-WidgetStatus {
    try {
        $currentTime = Get-Date
        
        # Market Status
        $marketOpen = $currentTime.TimeOfDay -ge [TimeSpan]"09:15:00" -and $currentTime.TimeOfDay -le [TimeSpan]"15:30:00"
        $preMarket = $currentTime.TimeOfDay -ge [TimeSpan]"09:00:00" -and $currentTime.TimeOfDay -lt [TimeSpan]"09:15:00"
        
        if ($marketOpen) {
            $marketLabel.Text = "MARKET OPEN"
            $marketLabel.ForeColor = [System.Drawing.Color]::LimeGreen
        } elseif ($preMarket) {
            $marketLabel.Text = "PRE-MARKET"
            $marketLabel.ForeColor = [System.Drawing.Color]::Gold
        } else {
            $marketLabel.Text = "MARKET CLOSED"
            $marketLabel.ForeColor = [System.Drawing.Color]::IndianRed
        }
        
        $timeLabel.Text = "Time: $($currentTime.ToString('dd-MMM-yyyy HH:mm:ss'))"
        
        # Database Connection
        $connectionString = "Server=.;Database=PalindromeResults;Trusted_Connection=True;TrustServerCertificate=True;"
        
        try {
            $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
            $connection.Open()
            
            # Authentication Status
            $authQuery = "SELECT COUNT(*) FROM AuthenticationTokens WHERE IsActive = 1 AND ExpiryTime > GETDATE()"
            $authCmd = New-Object System.Data.SqlClient.SqlCommand($authQuery, $connection)
            $activeTokens = $authCmd.ExecuteScalar()
            
            if ($activeTokens -gt 0) {
                $authLabel.Text = "Authentication: ACTIVE`nToken Valid & Ready"
                $authLabel.ForeColor = [System.Drawing.Color]::LimeGreen
            } else {
                $authLabel.Text = "Authentication: EXPIRED`nRun Daily Auth (DailyAuth.bat)"
                $authLabel.ForeColor = [System.Drawing.Color]::IndianRed
            }
            
            # Today's Data
            $todayQuery = @"
                SELECT 
                    COUNT(*) as TodaySnapshots,
                    (SELECT COUNT(*) FROM CircuitLimitTrackers WHERE CAST(DetectedAt AS DATE) = CAST(GETDATE() AS DATE)) as TodayCircuits,
                    (SELECT MAX(CaptureTime) FROM IntradayOptionSnapshots WHERE CAST(CaptureTime AS DATE) = CAST(GETDATE() AS DATE)) as LastCapture
                FROM IntradayOptionSnapshots 
                WHERE CAST(CaptureTime AS DATE) = CAST(GETDATE() AS DATE)
"@
            $todayCmd = New-Object System.Data.SqlClient.SqlCommand($todayQuery, $connection)
            $reader = $todayCmd.ExecuteReader()
            if ($reader.Read()) {
                $snapshots = $reader["TodaySnapshots"]
                $circuits = $reader["TodayCircuits"]
                $lastCapture = if ($reader["LastCapture"] -ne [DBNull]::Value) { $reader["LastCapture"] } else { "No data" }
                $todayLabel.Text = @"
TODAY'S ACTIVITY:
- Snapshots: $snapshots
- Circuit Changes: $circuits
- Last Capture: $lastCapture
"@
                $todayLabel.ForeColor = [System.Drawing.Color]::Cyan
            }
            $reader.Close()
            
            # Database Statistics
            $statsQuery = @"
                SELECT 
                    (SELECT COUNT(*) FROM IntradayOptionSnapshots) as TotalSnapshots,
                    (SELECT COUNT(*) FROM CircuitLimitTrackers) as TotalCircuits,
                    (SELECT COUNT(*) FROM HistoricalOptionData) as HistoricalRecords,
                    (SELECT COUNT(*) FROM AuthenticationTokens) as AuthTokens
"@
            $statsCmd = New-Object System.Data.SqlClient.SqlCommand($statsQuery, $connection)
            $statsReader = $statsCmd.ExecuteReader()
            if ($statsReader.Read()) {
                $totalSnaps = $statsReader["TotalSnapshots"]
                $totalCircuits = $statsReader["TotalCircuits"]
                $historicalRecords = $statsReader["HistoricalRecords"]
                $authTokens = $statsReader["AuthTokens"]
                $dbLabel.Text = @"
DATABASE STATISTICS:
- Total Snapshots: $totalSnaps
- Circuit Trackers: $totalCircuits
- Historical Data: $historicalRecords
- Auth Tokens: $authTokens
- Status: Connected
"@
                $dbLabel.ForeColor = [System.Drawing.Color]::White
            }
            $statsReader.Close()
            
            # Circuit Monitoring
            $recentCircuitsQuery = @"
                SELECT TOP 3 Symbol, DetectedAt, UpperCircuitLimit, LowerCircuitLimit
                FROM CircuitLimitTrackers 
                WHERE CAST(DetectedAt AS DATE) = CAST(GETDATE() AS DATE)
                ORDER BY DetectedAt DESC
"@
            $circuitCmd = New-Object System.Data.SqlClient.SqlCommand($recentCircuitsQuery, $connection)
            $circuitReader = $circuitCmd.ExecuteReader()
            $circuitText = "RECENT CIRCUIT CHANGES:`n"
            $count = 0
            while ($circuitReader.Read() -and $count -lt 3) {
                $symbol = $circuitReader["Symbol"]
                $time = ([DateTime]$circuitReader["DetectedAt"]).ToString("HH:mm")
                $circuitText += "- $symbol at $time`n"
                $count++
            }
            if ($count -eq 0) {
                $circuitText += "- No changes today"
            }
            $circuitReader.Close()
            $circuitLabel.Text = $circuitText
            $circuitLabel.ForeColor = [System.Drawing.Color]::Yellow
            
            # System Health
            $dotnetProcesses = @(Get-Process | Where-Object {$_.ProcessName -like "*dotnet*" -or $_.ProcessName -like "*OptionAnalysis*"}).Count
            $systemLabel.Text = @"
SYSTEM HEALTH:
- Database: Connected
- .NET Processes: $dotnetProcesses
- Widget: Active
"@
            $systemLabel.ForeColor = [System.Drawing.Color]::LimeGreen
            
            $connection.Close()
            
        } catch {
            $authLabel.Text = "Authentication: Unknown`nDatabase connection failed"
            $authLabel.ForeColor = [System.Drawing.Color]::Orange
            $dbLabel.Text = "DATABASE:`nConnection Failed`nCheck SQL Server Status"
            $dbLabel.ForeColor = [System.Drawing.Color]::IndianRed
            $systemLabel.Text = "SYSTEM:`nDatabase Error`nCheck Configuration"
            $systemLabel.ForeColor = [System.Drawing.Color]::IndianRed
        }
        
    } catch {
        $systemLabel.Text = "SYSTEM:`nWidget Error: $($_.Exception.Message)"
        $systemLabel.ForeColor = [System.Drawing.Color]::IndianRed
    }
}

# Button Events
$authButton.Add_Click({ 
    Start-Process -FilePath ".\DailyAuth.bat" -WorkingDirectory (Get-Location)
})

$startButton.Add_Click({
    Start-Process -FilePath ".\StartLiveMonitoring.bat" -WorkingDirectory (Get-Location)
})

$refreshButton.Add_Click({ 
    Update-WidgetStatus 
})

$helpButton.Add_Click({
    if (Test-Path ".\OPTION_ANALYSIS_COMPREHENSIVE_HELP.chm") {
        Start-Process ".\OPTION_ANALYSIS_COMPREHENSIVE_HELP.chm"
    } else {
        [System.Windows.Forms.MessageBox]::Show("Help file not found. Creating help documentation...", "Help", [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Information)
    }
})

# Auto-refresh timer
$timer = New-Object System.Windows.Forms.Timer
$timer.Interval = 5000  # 5 seconds
$timer.Add_Tick({ Update-WidgetStatus })

$widget.Add_Load({
    Update-WidgetStatus
    $timer.Start()
})

$widget.Add_FormClosed({
    $timer.Stop()
    $timer.Dispose()
})

# Enable dragging
$isDragging = $false
$lastLocation = New-Object System.Drawing.Point(0, 0)

$widget.Add_MouseDown({
    param($sender, $e)
    if ($e.Button -eq [System.Windows.Forms.MouseButtons]::Left) {
        $script:isDragging = $true
        $script:lastLocation = $e.Location
    }
})

$widget.Add_MouseMove({
    param($sender, $e)
    if ($script:isDragging) {
        $widget.Location = New-Object System.Drawing.Point(
            ($widget.Location.X + $e.X - $script:lastLocation.X),
            ($widget.Location.Y + $e.Y - $script:lastLocation.Y)
        )
    }
})

$widget.Add_MouseUp({
    param($sender, $e)
    $script:isDragging = $false
})

# Show widget
Write-Host "🚀 Starting Enhanced Desktop Widget..." -ForegroundColor Green
Write-Host "📱 Widget positioned at 25% of desktop width (right side)" -ForegroundColor Yellow
Write-Host "✨ Features:" -ForegroundColor Cyan
Write-Host "   🔄 Auto-refresh every 5 seconds" -ForegroundColor White
Write-Host "   🖱️ Drag to reposition" -ForegroundColor White
Write-Host "   📊 Real-time market & data status" -ForegroundColor White
Write-Host "   🎯 Circuit limit monitoring" -ForegroundColor White
Write-Host "   💾 Database statistics" -ForegroundColor White

$widget.ShowDialog() | Out-Null 