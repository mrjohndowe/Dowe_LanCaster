$ErrorActionPreference = "SilentlyContinue"
$Root = Split-Path -Parent $PSScriptRoot
$Bundled = Join-Path $Root "tools\ffmpeg\ffmpeg.exe"

if (Test-Path $Bundled) {
    Write-Host "FFmpeg found: $Bundled"
    & $Bundled -version | Select-Object -First 1
    exit 0
}

$cmd = Get-Command ffmpeg.exe
if ($cmd) {
    Write-Host "FFmpeg found on PATH: $($cmd.Source)"
    & $cmd.Source -version | Select-Object -First 1
    exit 0
}

Write-Host "FFmpeg not found."
Write-Host "Place ffmpeg.exe in tools\ffmpeg or add FFmpeg to PATH."
exit 1
