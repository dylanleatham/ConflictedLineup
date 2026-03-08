# Start both backend and frontend for local development.
# Usage: .\dev.ps1
#   Backend: http://localhost:8080
#   Frontend: http://localhost:5175 (proxies /api to backend)

$Root = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "Starting backend on http://localhost:8080..." -ForegroundColor Cyan
$backend = Start-Process -NoNewWindow -PassThru -FilePath "dotnet" `
    -ArgumentList "run" `
    -WorkingDirectory "$Root\backend\src\ConflictedLineup.Api"

Write-Host "Starting frontend on http://localhost:5175..." -ForegroundColor Cyan
$frontend = Start-Process -NoNewWindow -PassThru -FilePath "cmd.exe" `
    -ArgumentList "/c", "npm run dev" `
    -WorkingDirectory "$Root\frontend"

Write-Host ""
Write-Host "Both servers running. Press Ctrl+C to stop." -ForegroundColor Green

try {
    Wait-Process -Id $backend.Id, $frontend.Id
} finally {
    Write-Host "Shutting down..." -ForegroundColor Yellow
    Stop-Process -Id $backend.Id -ErrorAction SilentlyContinue
    Stop-Process -Id $frontend.Id -ErrorAction SilentlyContinue
}
