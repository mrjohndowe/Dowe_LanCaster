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

$TempZip = Join-Path $env:TEMP "dowe-lancaster-ffmpeg.zip"
$TempDir = Join-Path $env:TEMP "dowe-lancaster-ffmpeg"

$DownloadUrls = @(
    "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip",
    "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip"
)

Remove-Item $TempZip -Force -ErrorAction SilentlyContinue

$downloaded = $false
$downloadErrors = [System.Collections.Generic.List[string]]::new()

foreach ($downloadUrl in $DownloadUrls) {
    for ($attempt = 1; $attempt -le 3; $attempt++) {
        try {
            Write-Host "Downloading FFmpeg (attempt $attempt of 3): $downloadUrl"
            Invoke-WebRequest -Uri $downloadUrl -OutFile $TempZip -UseBasicParsing
            $downloaded = $true
            break
        }
        catch {
            Remove-Item $TempZip -Force -ErrorAction SilentlyContinue
            $downloadErrors.Add("$downloadUrl (attempt $attempt): $($_.Exception.Message)")

            if ($attempt -lt 3) {
                Start-Sleep -Seconds (2 * $attempt)
            }
        }
    }

    if ($downloaded) {
        break
    }
}

if (-not $downloaded) {
    throw "Could not download FFmpeg after retrying both providers.$([Environment]::NewLine)$($downloadErrors -join [Environment]::NewLine)"
}

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
