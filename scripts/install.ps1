#Requires -Version 5.1
$ErrorActionPreference = "Stop"

Write-Host "Installing Portless.NET..." -ForegroundColor Green
Write-Host ""

# Check if .NET SDK is installed
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "Error: .NET SDK is not installed." -ForegroundColor Red
    Write-Host "Please install .NET SDK from https://dotnet.microsoft.com/download"
    exit 1
}

# Install from NuGet.org
Write-Host "Installing Portless.NET.Tool from NuGet..." -ForegroundColor Cyan
dotnet tool install --global Portless.NET.Tool --version 1.0.0

# Add to PATH (persistent)
$toolPath = "$env:USERPROFILE\.dotnet\tools"
$pathEnv = [Environment]::GetEnvironmentVariable("Path", "User")

if ($pathEnv -notlike "*$toolPath*") {
    Write-Host ""
    Write-Host "Adding $toolPath to user PATH..." -ForegroundColor Cyan

    [Environment]::SetEnvironmentVariable("Path", "$pathEnv;$toolPath", "User")
    $env:Path = "$env:Path;$toolPath"

    Write-Host "Added to user PATH (persistent across restarts)"
    Write-Host "Please restart your terminal for PATH changes to take effect"
}

Write-Host ""
# Verify installation
if (Get-Command portless -ErrorAction SilentlyContinue) {
    Write-Host "Portless installed successfully!" -ForegroundColor Green
    Write-Host ""

    try {
        portless --version
    } catch {
        Write-Host "Version: 1.0.0"
    }

    Write-Host ""
    Write-Host "Getting started:" -ForegroundColor Cyan
    Write-Host "  1. Start the proxy: portless proxy start"
    Write-Host "  2. Run your app:    portless myapp dotnet run"
    Write-Host "  3. Access via URL:  http://myapp.localhost"
} else {
    Write-Host "Installation completed, but 'portless' command not found in PATH." -ForegroundColor Yellow
    Write-Host "Please restart your terminal for PATH changes to take effect"
    exit 1
}
