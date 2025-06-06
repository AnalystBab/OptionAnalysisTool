# Script to clear NuGet cache and kill locking processes

# Function to check and request admin privileges
function Request-AdminPrivileges {
    if (-not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        Write-Host "Requesting administrator privileges..." -ForegroundColor Yellow
        Start-Process powershell.exe -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`"" -Verb RunAs
        exit
    }
}

# Function to kill processes that might be locking files
function Kill-LockingProcesses {
    Write-Host "Killing processes that might be locking files..." -ForegroundColor Yellow
    $processes = @(
        "devenv",      # Visual Studio
        "msbuild",     # MSBuild
        "dotnet",      # .NET CLI
        "vcpkgsrv",    # Visual Studio Package Manager
        "ServiceHub"   # Visual Studio Service Hub
    )

    foreach ($process in $processes) {
        $runningProcesses = Get-Process -Name $process -ErrorAction SilentlyContinue
        if ($runningProcesses) {
            Write-Host "Found $($runningProcesses.Count) instances of $process" -ForegroundColor Yellow
            foreach ($proc in $runningProcesses) {
                try {
                    Write-Host "Killing process: $($proc.Id) - $($proc.ProcessName)" -ForegroundColor Yellow
                    Stop-Process -Id $proc.Id -Force -ErrorAction Stop
                    Write-Host "Successfully killed process" -ForegroundColor Green
                }
                catch {
                    Write-Host "Failed to kill process: $($proc.Id) - $($proc.ProcessName)" -ForegroundColor Red
                }
            }
        }
    }
    Start-Sleep -Seconds 2
}

# Function to clear NuGet cache
function Clear-NuGetCache {
    Write-Host "Clearing NuGet cache..." -ForegroundColor Yellow
    
    # Define NuGet cache paths
    $nugetPaths = @(
        "$env:USERPROFILE\.nuget\packages",
        "$env:USERPROFILE\AppData\Local\NuGet\v3-cache",
        "$env:USERPROFILE\AppData\Local\Temp\NuGetScratch",
        "$env:USERPROFILE\AppData\Local\NuGet\plugins-cache",
        "$env:USERPROFILE\AppData\Local\Microsoft\VisualStudio\*\ComponentModelCache",
        "$env:USERPROFILE\AppData\Local\Microsoft\VisualStudio\*\Designer"
    )

    # Kill processes first
    Kill-LockingProcesses

    # Try to clear each path
    foreach ($path in $nugetPaths) {
        if (Test-Path $path) {
            Write-Host "Cleaning: $path" -ForegroundColor Yellow
            try {
                Remove-Item -Path $path -Recurse -Force -ErrorAction Stop
                Write-Host "Successfully cleaned: $path" -ForegroundColor Green
            }
            catch {
                Write-Host "Failed to clean: $path" -ForegroundColor Red
                Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
            }
        }
    }

    # Try dotnet clear command
    Write-Host "Running dotnet nuget locals all --clear..." -ForegroundColor Yellow
    try {
        dotnet nuget locals all --clear
        Write-Host "Successfully cleared NuGet cache using dotnet command" -ForegroundColor Green
    }
    catch {
        Write-Host "Failed to clear NuGet cache using dotnet command" -ForegroundColor Red
        Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Main execution
try {
    # Request admin privileges
    Request-AdminPrivileges

    Write-Host "Starting NuGet cache clearing process..." -ForegroundColor Green
    
    # Clear NuGet cache
    Clear-NuGetCache

    Write-Host "NuGet cache clearing completed!" -ForegroundColor Green
}
catch {
    Write-Host "An error occurred: $_" -ForegroundColor Red
    Write-Host "Error details: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Stack trace: $($_.ScriptStackTrace)" -ForegroundColor Red
    exit 1
} 