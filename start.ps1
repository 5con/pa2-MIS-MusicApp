# Start script for Freelance Music Platform
# Starts backend API and frontend static server

param(
    [switch]$UseHttps,
    [switch]$Help
)

if ($Help) {
    Write-Host "Usage: .\start.ps1 [-UseHttps] [-Help]"
    Write-Host ""
    Write-Host "Options:"
    Write-Host "  -UseHttps    Use HTTPS for backend API (port 7107)"
    Write-Host "  -Help        Show this help message"
    Write-Host ""
    Write-Host "The application will start on:"
    Write-Host "  Backend API:  http://localhost:5271 (or https://localhost:7107 with -UseHttps)"
    Write-Host "  Frontend:     http://localhost:8000"
    exit 0
}

Write-Host "Starting Freelance Music Platform..." -ForegroundColor Green
Write-Host ""

# Kill any previously running backend processes
try {
    $backendProcesses = Get-Process backend -ErrorAction SilentlyContinue
    if ($backendProcesses) {
        Write-Host "Stopping existing backend processes..."
        $backendProcesses | Stop-Process -Force
        Start-Sleep -Milliseconds 500
    }
} catch {
    # Ignore errors
}

# Kill any processes on port 8000 (frontend server)
try {
    $frontendPort = Get-NetTCPConnection -LocalPort 8000 -ErrorAction SilentlyContinue
    if ($frontendPort) {
        $processId = $frontendPort.OwningProcess
        Write-Host "Stopping existing frontend server on port 8000..."
        Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
        Start-Sleep -Milliseconds 500
    }
} catch {
    # Ignore errors
}

# Check if we're in the right directory
if (-not (Test-Path "backend\backend.csproj")) {
    Write-Error "Error: backend.csproj not found. Please run this script from the project root directory."
    exit 1
}

# Check if .NET is installed
try {
    $dotnetVersion = dotnet --version 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet not found"
    }
    Write-Host ".NET Version: $dotnetVersion" -ForegroundColor Cyan
} catch {
    Write-Error "Error: .NET SDK is not installed or not in PATH."
    exit 1
}

# Check if Python is installed
try {
    $pythonVersion = python --version 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "python not found"
    }
    Write-Host "Python Version: $pythonVersion" -ForegroundColor Cyan
} catch {
    Write-Error "Error: Python is not installed or not in PATH. Please install Python to serve the frontend."
    exit 1
}

Write-Host ""

# Start backend API in background
try {
    Set-Location "backend"
    
    if ($UseHttps) {
        Write-Host "Starting Backend API with HTTPS on https://localhost:7107..." -ForegroundColor Yellow
        Start-Process -FilePath "dotnet" -ArgumentList "run", "--launch-profile", "https" -NoNewWindow -PassThru | Out-Null
    } else {
        Write-Host "Starting Backend API with HTTP on http://localhost:5271..." -ForegroundColor Yellow
        Start-Process -FilePath "dotnet" -ArgumentList "run", "--launch-profile", "http" -NoNewWindow -PassThru | Out-Null
    }
    
    Set-Location ".."
} catch {
    Write-Error "Failed to start backend API: $($_.Exception.Message)"
    exit 1
}

# Wait a moment for backend to start
Start-Sleep -Seconds 2

# Start frontend static server
try {
    Set-Location "frontend"
    
    Write-Host "Starting Frontend Server on http://localhost:8000..." -ForegroundColor Yellow
    Start-Process -FilePath "python" -ArgumentList "-m", "http.server", "8000" -NoNewWindow -PassThru | Out-Null
    
    Set-Location ".."
} catch {
    Write-Error "Failed to start frontend server: $($_.Exception.Message)"
    exit 1
}

Write-Host ""
Write-Host "==============================================" -ForegroundColor Green
Write-Host "Application started successfully!" -ForegroundColor Green
Write-Host "==============================================" -ForegroundColor Green
Write-Host ""
Write-Host "Frontend: http://localhost:8000" -ForegroundColor Cyan
if ($UseHttps) {
    Write-Host "Backend API: https://localhost:7107/api" -ForegroundColor Cyan
} else {
    Write-Host "Backend API: http://localhost:5271/api" -ForegroundColor Cyan
}
Write-Host ""
Write-Host "Press Ctrl+C to stop all servers" -ForegroundColor Yellow
Write-Host ""

# Keep script running
try {
    while ($true) {
        Start-Sleep -Seconds 1
    }
} finally {
    Write-Host "`nShutting down servers..." -ForegroundColor Yellow
    
    # Cleanup
    Get-Process backend -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    
    $frontendPort = Get-NetTCPConnection -LocalPort 8000 -ErrorAction SilentlyContinue
    if ($frontendPort) {
        Stop-Process -Id $frontendPort.OwningProcess -Force -ErrorAction SilentlyContinue
    }
    
    Write-Host "Servers stopped." -ForegroundColor Green
}
