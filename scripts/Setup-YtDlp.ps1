$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$ToolsDir = Join-Path $ProjectRoot "tools\yt-dlp"
$Exe = Join-Path $ToolsDir "yt-dlp.exe"

New-Item -ItemType Directory -Force -Path $ToolsDir | Out-Null

if (Test-Path $Exe) {
    Write-Host "Updating yt-dlp..."
    & $Exe -U
    exit $LASTEXITCODE
}

$Url = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe"

Write-Host "Downloading yt-dlp from the official GitHub release..."
Invoke-WebRequest -Uri $Url -OutFile $Exe -UseBasicParsing

Write-Host ""
Write-Host "yt-dlp installed:"
Write-Host $Exe
& $Exe --version
