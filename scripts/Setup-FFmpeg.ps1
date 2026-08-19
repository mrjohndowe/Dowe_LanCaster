$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$ToolsDir = Join-Path $ProjectRoot "tools\ffmpeg"
$FfmpegExe = Join-Path $ToolsDir "ffmpeg.exe"

if (Test-Path $FfmpegExe) {
    Write-Host "FFmpeg is already installed:"
    Write-Host $FfmpegExe
    exit 0
}

New-Item -ItemType Directory -Force -Path $ToolsDir | Out-Null

$DownloadUrl = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip"
$TempZip = Join-Path $env:TEMP "dowe-lancaster-ffmpeg.zip"
$TempDir = Join-Path $env:TEMP "dowe-lancaster-ffmpeg"

Write-Host "Downloading FFmpeg Windows essentials..."
Invoke-WebRequest -Uri $DownloadUrl -OutFile $TempZip -UseBasicParsing

if (Test-Path $TempDir) {
    Remove-Item $TempDir -Recurse -Force
}

Expand-Archive -Path $TempZip -DestinationPath $TempDir -Force

$BinDir = Get-ChildItem -Path $TempDir -Directory |
    Select-Object -First 1 |
    ForEach-Object { Join-Path $_.FullName "bin" }

if (-not (Test-Path $BinDir)) {
    throw "Could not locate the FFmpeg bin directory after extraction."
}

Copy-Item (Join-Path $BinDir "ffmpeg.exe") $ToolsDir -Force
Copy-Item (Join-Path $BinDir "ffprobe.exe") $ToolsDir -Force

$Ffplay = Join-Path $BinDir "ffplay.exe"
if (Test-Path $Ffplay) {
    Copy-Item $Ffplay $ToolsDir -Force
}

Remove-Item $TempZip -Force -ErrorAction SilentlyContinue
Remove-Item $TempDir -Recurse -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "FFmpeg installed to:"
Write-Host $ToolsDir
& $FfmpegExe -version | Select-Object -First 1
