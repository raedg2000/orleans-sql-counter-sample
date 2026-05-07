# Start-Dev.ps1
# Ensures Azurite (local Azure Storage emulator) is running before launching the solution.

$azuriteDataDir = "$PSScriptRoot\.azurite"
$tablePort = 10002

# Create data directory if it doesn't exist
if (-not (Test-Path $azuriteDataDir)) {
    New-Item -ItemType Directory -Path $azuriteDataDir | Out-Null
}

# Check if Azurite Table Storage port is already listening
$isRunning = (Test-NetConnection -ComputerName 127.0.0.1 -Port $tablePort -WarningAction SilentlyContinue).TcpTestSucceeded

if ($isRunning) {
    Write-Host "Azurite is already running on port $tablePort." -ForegroundColor Green
} else {
    Write-Host "Starting Azurite..." -ForegroundColor Yellow
    Start-Job -ScriptBlock {
        param($location)
        npx --yes azurite --silent --location $location
    } -ArgumentList $azuriteDataDir | Out-Null

    # Wait until Table Storage port is ready
    $timeout = 15
    $elapsed = 0
    do {
        Start-Sleep -Seconds 1
        $elapsed++
        $isRunning = (Test-NetConnection -ComputerName 127.0.0.1 -Port $tablePort -WarningAction SilentlyContinue).TcpTestSucceeded
    } while (-not $isRunning -and $elapsed -lt $timeout)

    if ($isRunning) {
        Write-Host "Azurite started successfully." -ForegroundColor Green
    } else {
        Write-Error "Azurite failed to start within $timeout seconds."
        exit 1
    }
}

Write-Host "Ready. Start the solution in Visual Studio using the 'New Profile' launch profile." -ForegroundColor Cyan
