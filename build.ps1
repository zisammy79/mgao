# MGAO Build Script (Windows PowerShell)
# Prerequisites: .NET 6.0 SDK, Outlook installed

$ErrorActionPreference = "Stop"

Write-Host "Building MGAO Solution..." -ForegroundColor Cyan

# Restore packages
Write-Host "Restoring NuGet packages..."
dotnet restore src/MGAO.sln

# Build solution
Write-Host "Building solution..."
dotnet build src/MGAO.sln --configuration Release --no-restore

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build successful!" -ForegroundColor Green
    Write-Host "Output: src/MGAO.UI/bin/Release/net6.0-windows/"
} else {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}
