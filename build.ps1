# Build script for Option Analysis Tool

# Function to check and request admin privileges
function Request-AdminPrivileges {
    if (-not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        Write-Host "Requesting administrator privileges..." -ForegroundColor Yellow
        Start-Process powershell.exe -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`"" -Verb RunAs
        exit
    }
}

# Function to handle errors
function Handle-Error {
    param($ErrorMessage)
    Write-Host $ErrorMessage -ForegroundColor Red
    Write-Host "Error details: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Stack trace: $($_.ScriptStackTrace)" -ForegroundColor Red
    exit 1
}

# Function to execute command and check result
function Execute-Command {
    param($Command, $ErrorMessage)
    Write-Host "Executing: $Command" -ForegroundColor Yellow
    try {
        $result = Invoke-Expression $Command 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Command output: $result" -ForegroundColor Red
            Handle-Error $ErrorMessage
        }
        return $result
    }
    catch {
        Write-Host "Command output: $result" -ForegroundColor Red
        Handle-Error $ErrorMessage
    }
}

# Function to force kill processes
function Force-KillProcesses {
    Write-Host "Killing processes that might be locking files..." -ForegroundColor Yellow
    $processes = @("devenv", "msbuild", "dotnet")
    foreach ($process in $processes) {
        Get-Process -Name $process -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    }
    Start-Sleep -Seconds 2
}

# Function to clear NuGet cache
function Clear-NuGetCache {
    Write-Host "Clearing NuGet cache..." -ForegroundColor Yellow
    try {
        # First try normal clear
        Execute-Command "dotnet nuget locals all --clear" "Failed to clear NuGet cache"
    }
    catch {
        Write-Host "Warning: Normal NuGet cache clear failed, trying force clear..." -ForegroundColor Yellow
        try {
            # Force kill processes
            Force-KillProcesses

            # Try to clear specific directories
            $nugetPaths = @(
                "$env:USERPROFILE\.nuget\packages",
                "$env:USERPROFILE\AppData\Local\NuGet\v3-cache",
                "$env:USERPROFILE\AppData\Local\Temp\NuGetScratch",
                "$env:USERPROFILE\AppData\Local\NuGet\plugins-cache"
            )

            foreach ($path in $nugetPaths) {
                if (Test-Path $path) {
                    Write-Host "Cleaning: $path" -ForegroundColor Yellow
                    Remove-Item -Path $path -Recurse -Force -ErrorAction SilentlyContinue
                }
            }

            # Try dotnet clear again
            Execute-Command "dotnet nuget locals all --clear" "Failed to clear NuGet cache even after force clear"
        }
        catch {
            Write-Host "Warning: Force NuGet cache clear failed, but continuing..." -ForegroundColor Yellow
        }
    }
}

# Function to clean solution
function Clean-Solution {
    Write-Host "Cleaning solution..." -ForegroundColor Yellow
    try {
        # First try to clean bin and obj directories manually
        Write-Host "Cleaning bin and obj directories..." -ForegroundColor Yellow
        Get-ChildItem -Path . -Include bin,obj -Recurse -Directory | ForEach-Object {
            try {
                Remove-Item -Path $_.FullName -Recurse -Force -ErrorAction Stop
                Write-Host "Cleaned: $($_.FullName)" -ForegroundColor Green
            }
            catch {
                Write-Host "Warning: Could not clean $($_.FullName)" -ForegroundColor Yellow
            }
        }

        # Then try dotnet clean
        Write-Host "Running dotnet clean..." -ForegroundColor Yellow
        Execute-Command "dotnet clean" "Failed to clean solution"
    }
    catch {
        Write-Host "Warning: Clean operation failed, but continuing..." -ForegroundColor Yellow
    }
}

# Function to restore and build solution
function Build-Solution {
    Write-Host "Restoring packages..." -ForegroundColor Yellow
    Execute-Command "dotnet restore --force" "Failed to restore packages"

    Write-Host "Building solution..." -ForegroundColor Yellow
    Execute-Command "dotnet build" "Failed to build solution"

    Write-Host "Running tests..." -ForegroundColor Yellow
    Execute-Command "dotnet test" "Tests failed"
}

# Main execution
try {
    # Request admin privileges
    Request-AdminPrivileges

    Write-Host "Starting build process..." -ForegroundColor Green

    # Check if solution file exists
    if (-not (Test-Path "OptionAnalysisTool.sln")) {
        Handle-Error "Solution file not found. Please run this script from the correct directory."
    }

    # Clean solution
    Clean-Solution

    # Build solution
    Build-Solution

    Write-Host "Build completed successfully!" -ForegroundColor Green
}
catch {
    Handle-Error "An error occurred: $_"
} 