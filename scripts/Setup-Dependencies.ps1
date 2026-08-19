$ErrorActionPreference = "Stop"

$Here = $PSScriptRoot

Write-Host "==================================="
Write-Host " Dowe LanCaster Dependency Setup"
Write-Host "==================================="
Write-Host ""

& (Join-Path $Here "Setup-FFmpeg.ps1")
if ($LASTEXITCODE -ne 0) {
    throw "FFmpeg setup failed."
}

Write-Host ""
& (Join-Path $Here "Setup-YtDlp.ps1")
if ($LASTEXITCODE -ne 0) {
    throw "yt-dlp setup failed."
}

Write-Host ""
Write-Host "Dependencies are ready."
