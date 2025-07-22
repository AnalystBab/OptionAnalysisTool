# Professional Desktop Widget for Option Analysis Tool
# Creates a desktop overlay widget that stays on desktop background

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Win32 {
    [DllImport("user32.dll")]
    public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
    
    [DllImport("user32.dll")]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
    
    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    
    [DllImport("user32.dll")]
    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    
    [DllImport("user32.dll")]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    
    public const int GWL_EXSTYLE = -20;
    public const int WS_EX_TOOLWINDOW = 0x00000080;
    public const int WS_EX_TOPMOST = 0x00000008;
    public const uint SWP_NOACTIVATE = 0x0010;
    public const uint SWP_NOMOVE = 0x0002;
    public const uint SWP_NOSIZE = 0x0001;
    public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
    public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
}
"@

function Get-SystemStatus {
    $status = @{}
    
    # Check Authentication
    try {
        $configPath = ".\appsettings.json"
        if (Test-Path $configPath) {
            $config = Get-Content $configPath | ConvertFrom-Json
            if ($config.KiteConnect.AccessToken -and $config.KiteConnect.AccessToken -ne "your_kite_access_token_here") {
                $status.Auth = "✓ AUTH"
                $status.AuthColor = [System.Drawing.Color]::LimeGreen
            } else {
                $status.Auth = "✗ AUTH"
                $status.AuthColor = [System.Drawing.Color]::Red
            }
        } else {
            $status.Auth = "? AUTH"
            $status.AuthColor = [System.Drawing.Color]::Orange
        }
    } catch {
        $status.Auth = "! AUTH"
        $status.AuthColor = [System.Drawing.Color]::Red
    }
    
    # Check Service
    $processes = Get-Process -Name "OptionAnalysisTool.Console" -ErrorAction SilentlyContinue
    if ($processes) {
        $status.Service = "✓ DATA"
        $status.ServiceColor = [System.Drawing.Color]::LimeGreen
    } else {
        $status.Service = "✗ DATA"
        $status.ServiceColor = [System.Drawing.Color]::Red
    }
    
    # Check Market
    $now = Get-Date
    $marketOpen = Get-Date -Hour 9 -Minute 15 -Second 0
    $marketClose = Get-Date -Hour 15 -Minute 30 -Second 0
    
    if ($now -ge $marketOpen -and $now -le $marketClose) {
        $status.Market = "✓ OPEN"
        $status.MarketColor = [System.Drawing.Color]::LimeGreen
    } elseif ($now -lt $marketOpen) {
        $timeToOpen = $marketOpen - $now
        $status.Market = "⏱ $($timeToOpen.Hours)h$($timeToOpen.Minutes)m"
        $status.MarketColor = [System.Drawing.Color]::Gold
    } else {
        $status.Market = "✗ CLOSED"
        $status.MarketColor = [System.Drawing.Color]::Gray
    }
    
    $status.Time = Get-Date -Format "HH:mm:ss"
    return $status
}

# Create the desktop widget form
$widget = New-Object System.Windows.Forms.Form
$widget.Text = "OptionWidget"
$widget.Size = New-Object System.Drawing.Size(200, 120)
$widget.StartPosition = "Manual"

# Position in top-right corner of screen
$screen = [System.Windows.Forms.Screen]::PrimaryScreen.WorkingArea
$widget.Location = New-Object System.Drawing.Point(($screen.Width - 210), 10)

# Make it a desktop widget
$widget.FormBorderStyle = [System.Windows.Forms.FormBorderStyle]::None
$widget.TopMost = $true
$widget.ShowInTaskbar = $false
$widget.BackColor = [System.Drawing.Color]::FromArgb(30, 30, 30)  # Dark semi-transparent
$widget.Opacity = 0.85

# Add rounded corners effect
$widget.Region = [System.Drawing.Region]::FromHrgn([System.Drawing.Graphics]::FromHwnd($widget.Handle).GetHdc())

# Create a custom paint event for modern look
$widget.Add_Paint({
    param($sender, $e)
    $g = $e.Graphics
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    
    # Draw rounded rectangle background
    $rect = New-Object System.Drawing.Rectangle(2, 2, ($widget.Width - 4), ($widget.Height - 4))
    $brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(200, 20, 20, 25))
    $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(100, 70, 130, 180), 1)
    
    # Draw rounded rectangle
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $radius = 8
    $path.AddArc($rect.X, $rect.Y, $radius, $radius, 180, 90)
    $path.AddArc($rect.Right - $radius, $rect.Y, $radius, $radius, 270, 90)
    $path.AddArc($rect.Right - $radius, $rect.Bottom - $radius, $radius, $radius, 0, 90)
    $path.AddArc($rect.X, $rect.Bottom - $radius, $radius, $radius, 90, 90)
    $path.CloseFigure()
    
    $g.FillPath($brush, $path)
    $g.DrawPath($pen, $path)
    
    $brush.Dispose()
    $pen.Dispose()
    $path.Dispose()
})

# Title label
$titleLabel = New-Object System.Windows.Forms.Label
$titleLabel.Location = New-Object System.Drawing.Point(10, 5)
$titleLabel.Size = New-Object System.Drawing.Size(180, 15)
$titleLabel.Text = "OPTION ANALYSIS"
$titleLabel.Font = New-Object System.Drawing.Font("Segoe UI", 8, [System.Drawing.FontStyle]::Bold)
$titleLabel.ForeColor = [System.Drawing.Color]::White
$titleLabel.BackColor = [System.Drawing.Color]::Transparent
$titleLabel.TextAlign = [System.Drawing.ContentAlignment]::MiddleCenter

# Status labels
$authLabel = New-Object System.Windows.Forms.Label
$authLabel.Location = New-Object System.Drawing.Point(10, 25)
$authLabel.Size = New-Object System.Drawing.Size(80, 15)
$authLabel.Font = New-Object System.Drawing.Font("Consolas", 8, [System.Drawing.FontStyle]::Bold)
$authLabel.BackColor = [System.Drawing.Color]::Transparent

$serviceLabel = New-Object System.Windows.Forms.Label
$serviceLabel.Location = New-Object System.Drawing.Point(95, 25)
$serviceLabel.Size = New-Object System.Drawing.Size(80, 15)
$serviceLabel.Font = New-Object System.Drawing.Font("Consolas", 8, [System.Drawing.FontStyle]::Bold)
$serviceLabel.BackColor = [System.Drawing.Color]::Transparent

$marketLabel = New-Object System.Windows.Forms.Label
$marketLabel.Location = New-Object System.Drawing.Point(10, 45)
$marketLabel.Size = New-Object System.Drawing.Size(80, 15)
$marketLabel.Font = New-Object System.Drawing.Font("Consolas", 8, [System.Drawing.FontStyle]::Bold)
$marketLabel.BackColor = [System.Drawing.Color]::Transparent

$timeLabel = New-Object System.Windows.Forms.Label
$timeLabel.Location = New-Object System.Drawing.Point(95, 45)
$timeLabel.Size = New-Object System.Drawing.Size(80, 15)
$timeLabel.Font = New-Object System.Drawing.Font("Consolas", 8)
$timeLabel.ForeColor = [System.Drawing.Color]::LightGray
$timeLabel.BackColor = [System.Drawing.Color]::Transparent

# Quick action buttons (small)
$authBtn = New-Object System.Windows.Forms.Button
$authBtn.Location = New-Object System.Drawing.Point(10, 70)
$authBtn.Size = New-Object System.Drawing.Size(35, 20)
$authBtn.Text = "Auth"
$authBtn.Font = New-Object System.Drawing.Font("Segoe UI", 7)
$authBtn.BackColor = [System.Drawing.Color]::FromArgb(70, 130, 180)
$authBtn.ForeColor = [System.Drawing.Color]::White
$authBtn.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
$authBtn.FlatAppearance.BorderSize = 0
$authBtn.Add_Click({
    Start-Process "powershell" -ArgumentList "-ExecutionPolicy Bypass -File `"$PWD\DailyAuth.ps1`""
})

$startBtn = New-Object System.Windows.Forms.Button
$startBtn.Location = New-Object System.Drawing.Point(50, 70)
$startBtn.Size = New-Object System.Drawing.Size(35, 20)
$startBtn.Text = "Start"
$startBtn.Font = New-Object System.Drawing.Font("Segoe UI", 7)
$startBtn.BackColor = [System.Drawing.Color]::FromArgb(34, 139, 34)
$startBtn.ForeColor = [System.Drawing.Color]::White
$startBtn.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
$startBtn.FlatAppearance.BorderSize = 0
$startBtn.Add_Click({
    Start-Process "cmd" -ArgumentList "/k `"cd /d `"$PWD`" && dotnet run --project OptionAnalysisTool.Console`""
})

$hideBtn = New-Object System.Windows.Forms.Button
$hideBtn.Location = New-Object System.Drawing.Point(90, 70)
$hideBtn.Size = New-Object System.Drawing.Size(35, 20)
$hideBtn.Text = "Hide"
$hideBtn.Font = New-Object System.Drawing.Font("Segoe UI", 7)
$hideBtn.BackColor = [System.Drawing.Color]::FromArgb(139, 69, 19)
$hideBtn.ForeColor = [System.Drawing.Color]::White
$hideBtn.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
$hideBtn.FlatAppearance.BorderSize = 0
$hideBtn.Add_Click({
    $widget.WindowState = [System.Windows.Forms.FormWindowState]::Minimized
})

$closeBtn = New-Object System.Windows.Forms.Button
$closeBtn.Location = New-Object System.Drawing.Point(130, 70)
$closeBtn.Size = New-Object System.Drawing.Size(35, 20)
$closeBtn.Text = "Close"
$closeBtn.Font = New-Object System.Drawing.Font("Segoe UI", 7)
$closeBtn.BackColor = [System.Drawing.Color]::FromArgb(178, 34, 34)
$closeBtn.ForeColor = [System.Drawing.Color]::White
$closeBtn.FlatStyle = [System.Windows.Forms.FlatStyle]::Flat
$closeBtn.FlatAppearance.BorderSize = 0
$closeBtn.Add_Click({
    $widget.Close()
})

# Add context menu for right-click options
$contextMenu = New-Object System.Windows.Forms.ContextMenuStrip
$refreshItem = New-Object System.Windows.Forms.ToolStripMenuItem
$refreshItem.Text = "Refresh Status"
$refreshItem.Add_Click({ Update-WidgetStatus })

$hideItem = New-Object System.Windows.Forms.ToolStripMenuItem
$hideItem.Text = "Hide Widget"
$hideItem.Add_Click({ $widget.WindowState = [System.Windows.Forms.FormWindowState]::Minimized })

$exitItem = New-Object System.Windows.Forms.ToolStripMenuItem
$exitItem.Text = "Exit Widget"
$exitItem.Add_Click({ $widget.Close() })

$contextMenu.Items.Add($refreshItem)
$contextMenu.Items.Add($hideItem)
$contextMenu.Items.Add("-")  # Separator
$contextMenu.Items.Add($exitItem)

$widget.ContextMenuStrip = $contextMenu

# Add all controls
$widget.Controls.Add($titleLabel)
$widget.Controls.Add($authLabel)
$widget.Controls.Add($serviceLabel)
$widget.Controls.Add($marketLabel)
$widget.Controls.Add($timeLabel)
$widget.Controls.Add($authBtn)
$widget.Controls.Add($startBtn)
$widget.Controls.Add($hideBtn)
$widget.Controls.Add($closeBtn)

# Function to update widget status
function Update-WidgetStatus {
    $status = Get-SystemStatus
    
    $authLabel.Text = $status.Auth
    $authLabel.ForeColor = $status.AuthColor
    
    $serviceLabel.Text = $status.Service
    $serviceLabel.ForeColor = $status.ServiceColor
    
    $marketLabel.Text = $status.Market
    $marketLabel.ForeColor = $status.MarketColor
    
    $timeLabel.Text = $status.Time
}

# Auto-refresh timer
$timer = New-Object System.Windows.Forms.Timer
$timer.Interval = 5000  # 5 seconds
$timer.Add_Tick({
    Update-WidgetStatus
})

# Make widget stay on desktop
$widget.Add_Load({
    # Remove from taskbar and make it a desktop widget
    $style = [Win32]::GetWindowLong($widget.Handle, [Win32]::GWL_EXSTYLE)
    $style = $style -bor [Win32]::WS_EX_TOOLWINDOW
    [Win32]::SetWindowLong($widget.Handle, [Win32]::GWL_EXSTYLE, $style)
    
    # Set initial status
    Update-WidgetStatus
    
    # Start auto-refresh
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
Write-Host "Starting Professional Desktop Widget..." -ForegroundColor Green
Write-Host "Widget appears in top-right corner of screen" -ForegroundColor Yellow
Write-Host "Features:" -ForegroundColor Cyan
Write-Host "- Drag to move around desktop" -ForegroundColor White
Write-Host "- Right-click for options menu" -ForegroundColor White
Write-Host "- Auto-refreshes every 5 seconds" -ForegroundColor White
Write-Host "- Click 'Hide' to minimize to system tray" -ForegroundColor White
Write-Host "- Click 'Close' to exit widget" -ForegroundColor White

$widget.Show()
$widget.BringToFront()

# Keep the script running
[System.Windows.Forms.Application]::Run($widget) 