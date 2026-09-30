# Start both backend and frontend for local development.
# Usage: .\dev.ps1         (real Claude + Spotify; needs backend/.env and frontend/.env.local)
#        .\dev.ps1 -Demo   (fictional festival, no keys needed)
#   Backend: http://localhost:8080
#   Frontend: http://localhost:5175 (proxies /api to backend)

param([switch]$Demo)

$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$LaunchProfile = if ($Demo) { "demo" } else { "http" }

Write-Host "Starting backend ($LaunchProfile) on http://localhost:8080..." -ForegroundColor Cyan
$backend = Start-Process -NoNewWindow -PassThru -FilePath "dotnet" `
    -ArgumentList "run", "--launch-profile", $LaunchProfile `
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
