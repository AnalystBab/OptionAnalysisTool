# Professional System Tray Widget for Option Analysis Tool
# Creates a system tray icon with status popup

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

function Get-SystemStatus {
    $status = @{
        AuthStatus = "Unknown"
        ServiceStatus = "Unknown"
        MarketStatus = "Unknown"
        AuthColor = "Gray"
        ServiceColor = "Gray"
        MarketColor = "Gray"
    }
    
    # Check Authentication
    try {
        $configPath = ".\appsettings.json"
        if (Test-Path $configPath) {
            $config = Get-Content $configPath | ConvertFrom-Json
            if ($config.KiteConnect.AccessToken -and $config.KiteConnect.AccessToken -ne "your_kite_access_token_here") {
                $status.AuthStatus = "Authenticated"
                $status.AuthColor = "Green"
            } else {
                $status.AuthStatus = "Not Authenticated"
                $status.AuthColor = "Red"
            }
        }
    } catch {
        $status.AuthStatus = "Config Error"
        $status.AuthColor = "Red"
    }
    
    # Check Service
    $processes = Get-Process -Name "OptionAnalysisTool.Console" -ErrorAction SilentlyContinue
    if ($processes) {
        $status.ServiceStatus = "Data Collection Running"
        $status.ServiceColor = "Green"
    } else {
        $status.ServiceStatus = "Data Collection Stopped"
        $status.ServiceColor = "Red"
    }
    
    # Check Market
    $now = Get-Date
    $marketOpen = Get-Date -Hour 9 -Minute 15 -Second 0
    $marketClose = Get-Date -Hour 15 -Minute 30 -Second 0
    
    if ($now -ge $marketOpen -and $now -le $marketClose) {
        $status.MarketStatus = "Market Open"
        $status.MarketColor = "Green"
    } elseif ($now -lt $marketOpen) {
        $timeToOpen = $marketOpen - $now
        $status.MarketStatus = "Opens in $($timeToOpen.Hours)h $($timeToOpen.Minutes)m"
        $status.MarketColor = "Orange"
    } else {
        $status.MarketStatus = "Market Closed"
        $status.MarketColor = "Gray"
    }
    
    return $status
}

function Create-TrayIcon {
    # Create a 16x16 icon dynamically based on status
    $status = Get-SystemStatus()
    $bitmap = New-Object System.Drawing.Bitmap(16, 16)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    
    # Background color based on overall status
    if ($status.AuthColor -eq "Green" -and $status.ServiceColor -eq "Green") {
        $bgColor = [System.Drawing.Color]::Green
    } elseif ($status.AuthColor -eq "Red" -or $status.ServiceColor -eq "Red") {
        $bgColor = [System.Drawing.Color]::Red
    } else {
        $bgColor = [System.Drawing.Color]::Orange
    }
    
    # Draw icon
    $graphics.Clear($bgColor)
    $graphics.FillEllipse([System.Drawing.Brushes]::White, 2, 2, 12, 12)
    $graphics.DrawString("O", (New-Object System.Drawing.Font("Arial", 8, [System.Drawing.FontStyle]::Bold)), [System.Drawing.Brushes]::Black, 4, 2)
    
    $icon = [System.Drawing.Icon]::FromHandle($bitmap.GetHicon())
    $graphics.Dispose()
    
    return $icon
}

# Create the main form (hidden)
$form = New-Object System.Windows.Forms.Form
$form.WindowState = [System.Windows.Forms.FormWindowState]::Minimized
$form.ShowInTaskbar = $false
$form.Visible = $false

# Create system tray icon
$trayIcon = New-Object System.Windows.Forms.NotifyIcon
$trayIcon.Text = "Option Analysis Tool"
$trayIcon.Icon = Create-TrayIcon
$trayIcon.Visible = $true

# Create status popup form
$statusForm = New-Object System.Windows.Forms.Form
$statusForm.Text = "Option Analysis Status"
$statusForm.Size = New-Object System.Drawing.Size(300, 200)
$statusForm.StartPosition = "Manual"
$statusForm.FormBorderStyle = [System.Windows.Forms.FormBorderStyle]::FixedToolWindow
$statusForm.TopMost = $true
$statusForm.ShowInTaskbar = $false
$statusForm.BackColor = [System.Drawing.Color]::FromArgb(40, 40, 40)
$statusForm.ForeColor = [System.Drawing.Color]::White

# Status labels in popup
$titleLabel = New-Object System.Windows.Forms.Label
$titleLabel.Location = New-Object System.Drawing.Point(10, 10)
$titleLabel.Size = New-Object System.Drawing.Size(270, 25)
$titleLabel.Text = "OPTION ANALYSIS TOOL STATUS"
$titleLabel.Font = New-Object System.Drawing.Font("Segoe UI", 10, [System.Drawing.FontStyle]::Bold)
$titleLabel.ForeColor = [System.Drawing.Color]::Cyan
$titleLabel.TextAlign = [System.Drawing.ContentAlignment]::MiddleCenter

$authStatusLabel = New-Object System.Windows.Forms.Label
$authStatusLabel.Location = New-Object System.Drawing.Point(20, 45)
$authStatusLabel.Size = New-Object System.Drawing.Size(250, 20)
$authStatusLabel.Font = New-Object System.Drawing.Font("Segoe UI", 9)

$serviceStatusLabel = New-Object System.Windows.Forms.Label
$serviceStatusLabel.Location = New-Object System.Drawing.Point(20, 70)
$serviceStatusLabel.Size = New-Object System.Drawing.Size(250, 20)
$serviceStatusLabel.Font = New-Object System.Drawing.Font("Segoe UI", 9)

$marketStatusLabel = New-Object System.Windows.Forms.Label
$marketStatusLabel.Location = New-Object System.Drawing.Point(20, 95)
$marketStatusLabel.Size = New-Object System.Drawing.Size(250, 20)
$marketStatusLabel.Font = New-Object System.Drawing.Font("Segoe UI", 9)

$timeLabel = New-Object System.Windows.Forms.Label
$timeLabel.Location = New-Object System.Drawing.Point(20, 120)
$timeLabel.Size = New-Object System.Drawing.Size(250, 20)
$timeLabel.Font = New-Object System.Drawing.Font("Segoe UI", 8)
$timeLabel.ForeColor = [System.Drawing.Color]::LightGray

# Quick action buttons
$authButton = New-Object System.Windows.Forms.Button
$authButton.Location = New-Object System.Drawing.Point(20, 150)
$authButton.Size = New-Object System.Drawing.Size(80, 25)
$authButton.Text = "Authenticate"
$authButton.BackColor = [System.Drawing.Color]::DarkBlue
$authButton.ForeColor = [System.Drawing.Color]::White
$authButton.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
$authButton.Add_Click({
    $statusForm.Hide()
    Start-Process "powershell" -ArgumentList "-ExecutionPolicy Bypass -File `"$PWD\DailyAuth.ps1`""
})

$startButton = New-Object System.Windows.Forms.Button
$startButton.Location = New-Object System.Drawing.Point(110, 150)
$startButton.Size = New-Object System.Drawing.Size(80, 25)
$startButton.Text = "Start Data"
$startButton.BackColor = [System.Drawing.Color]::DarkGreen
$startButton.ForeColor = [System.Drawing.Color]::White
$startButton.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
$startButton.Add_Click({
    $statusForm.Hide()
    Start-Process "cmd" -ArgumentList "/k `"cd /d `"$PWD`" && dotnet run --project OptionAnalysisTool.Console`""
})

$closeButton = New-Object System.Windows.Forms.Button
$closeButton.Location = New-Object System.Drawing.Point(200, 150)
$closeButton.Size = New-Object System.Drawing.Size(70, 25)
$closeButton.Text = "Close"
$closeButton.BackColor = [System.Drawing.Color]::DarkRed
$closeButton.ForeColor = [System.Drawing.Color]::White
$closeButton.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
$closeButton.Add_Click({
    $statusForm.Hide()
})

# Add controls to status form
$statusForm.Controls.Add($titleLabel)
$statusForm.Controls.Add($authStatusLabel)
$statusForm.Controls.Add($serviceStatusLabel)
$statusForm.Controls.Add($marketStatusLabel)
$statusForm.Controls.Add($timeLabel)
$statusForm.Controls.Add($authButton)
$statusForm.Controls.Add($startButton)
$statusForm.Controls.Add($closeButton)

# Create context menu for tray icon
$contextMenu = New-Object System.Windows.Forms.ContextMenuStrip

$showStatusItem = New-Object System.Windows.Forms.ToolStripMenuItem
$showStatusItem.Text = "Show Status"
$showStatusItem.Add_Click({
    Update-StatusDisplay
    $statusForm.Show()
    $statusForm.BringToFront()
})

$separatorItem1 = New-Object System.Windows.Forms.ToolStripSeparator

$authMenuItem = New-Object System.Windows.Forms.ToolStripMenuItem
$authMenuItem.Text = "Daily Authentication"
$authMenuItem.Add_Click({
    Start-Process "powershell" -ArgumentList "-ExecutionPolicy Bypass -File `"$PWD\DailyAuth.ps1`""
})

$startMenuItem = New-Object System.Windows.Forms.ToolStripMenuItem
$startMenuItem.Text = "Start Data Collection"
$startMenuItem.Add_Click({
    Start-Process "cmd" -ArgumentList "/k `"cd /d `"$PWD`" && dotnet run --project OptionAnalysisTool.Console`""
})

$separatorItem2 = New-Object System.Windows.Forms.ToolStripSeparator

$exitMenuItem = New-Object System.Windows.Forms.ToolStripMenuItem
$exitMenuItem.Text = "Exit"
$exitMenuItem.Add_Click({
    $trayIcon.Visible = $false
    $form.Close()
})

$contextMenu.Items.Add($showStatusItem)
$contextMenu.Items.Add($separatorItem1)
$contextMenu.Items.Add($authMenuItem)
$contextMenu.Items.Add($startMenuItem)
$contextMenu.Items.Add($separatorItem2)
$contextMenu.Items.Add($exitMenuItem)

$trayIcon.ContextMenuStrip = $contextMenu

# Function to update status display
function Update-StatusDisplay {
    $status = Get-SystemStatus
    
    # Update tray icon
    $trayIcon.Icon = Create-TrayIcon
    
    # Update popup labels
    $authStatusLabel.Text = "Authentication: $($status.AuthStatus)"
    $authStatusLabel.ForeColor = [System.Drawing.Color]::FromName($status.AuthColor)
    
    $serviceStatusLabel.Text = "Service: $($status.ServiceStatus)"
    $serviceStatusLabel.ForeColor = [System.Drawing.Color]::FromName($status.ServiceColor)
    
    $marketStatusLabel.Text = "Market: $($status.MarketStatus)"
    $marketStatusLabel.ForeColor = [System.Drawing.Color]::FromName($status.MarketColor)
    
    $timeLabel.Text = "Last Updated: $(Get-Date -Format 'HH:mm:ss')"
    
    # Update tray icon tooltip
    $trayIcon.Text = "Option Analysis Tool`nAuth: $($status.AuthStatus)`nService: $($status.ServiceStatus)`nMarket: $($status.MarketStatus)"
}

# Double-click to show status
$trayIcon.Add_DoubleClick({
    Update-StatusDisplay
    $statusForm.Show()
    $statusForm.BringToFront()
})

# Auto-refresh timer
$timer = New-Object System.Windows.Forms.Timer
$timer.Interval = 10000  # 10 seconds
$timer.Add_Tick({
    Update-StatusDisplay
})

# Initial status update
Update-StatusDisplay
$timer.Start()

# Handle form closing
$form.Add_FormClosed({
    $timer.Stop()
    $timer.Dispose()
    $trayIcon.Visible = $false
    $trayIcon.Dispose()
})

Write-Host "Professional System Tray Widget Started!" -ForegroundColor Green
Write-Host "Look for the Option Analysis icon in your system tray (bottom-right)" -ForegroundColor Yellow
Write-Host "Features:" -ForegroundColor Cyan
Write-Host "- System tray icon with color-coded status" -ForegroundColor White
Write-Host "- Double-click tray icon to show detailed status" -ForegroundColor White
Write-Host "- Right-click tray icon for quick actions" -ForegroundColor White
Write-Host "- Auto-updates every 10 seconds" -ForegroundColor White
Write-Host "- Professional integration with Windows" -ForegroundColor White

# Run the application
[System.Windows.Forms.Application]::Run($form) 