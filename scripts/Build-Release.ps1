$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$Project = Join-Path $ProjectRoot "src\DoweLanCaster.Windows\DoweLanCaster.Windows.csproj"
$Dist = Join-Path $ProjectRoot "dist"
$WindowsDist = Join-Path $Dist "DoweLanCaster-Windows"
$ReleaseZip = Join-Path $Dist "DoweLanCaster-Windows-x64.zip"
$FfmpegDir = Join-Path $ProjectRoot "tools\ffmpeg"
$YtDlpDir = Join-Path $ProjectRoot "tools\yt-dlp"
$RokuZip = Join-Path $Dist "DoweLanCaster-Roku.zip"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw ".NET SDK was not found. Install the .NET 8 SDK first."
}

if (-not (Test-Path (Join-Path $FfmpegDir "ffmpeg.exe")) -or
    -not (Test-Path (Join-Path $YtDlpDir "yt-dlp.exe"))) {
    Write-Host "Dependencies are missing. Running setup..."
    & (Join-Path $PSScriptRoot "Setup-Dependencies.ps1")
}

Write-Host ""
Write-Host "Packaging Roku receiver..."
& (Join-Path $PSScriptRoot "Package-Roku.ps1")

if (Test-Path $WindowsDist) {
    Remove-Item $WindowsDist -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $WindowsDist | Out-Null

Write-Host ""
Write-Host "Publishing Dowe LanCaster..."
dotnet publish $Project `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $WindowsDist

$PublishedFfmpeg = Join-Path $WindowsDist "tools\ffmpeg"
$PublishedYtDlp = Join-Path $WindowsDist "tools\yt-dlp"

New-Item -ItemType Directory -Force -Path $PublishedFfmpeg | Out-Null
New-Item -ItemType Directory -Force -Path $PublishedYtDlp | Out-Null

Copy-Item (Join-Path $FfmpegDir "ffmpeg.exe") $PublishedFfmpeg -Force
Copy-Item (Join-Path $FfmpegDir "ffprobe.exe") $PublishedFfmpeg -Force

if (Test-Path (Join-Path $FfmpegDir "ffplay.exe")) {
    Copy-Item (Join-Path $FfmpegDir "ffplay.exe") $PublishedFfmpeg -Force
}

Copy-Item (Join-Path $YtDlpDir "yt-dlp.exe") $PublishedYtDlp -Force
Copy-Item $RokuZip (Join-Path $WindowsDist "DoweLanCaster-Roku.zip") -Force

@"
Dowe LanCaster

Run DoweLanCaster.exe.

Features:
- Link Cast: paste a public/non-DRM video webpage URL
- Live Cast: desktop, monitor, or application window
- File Cast: local files
- Remote: Roku remote control and app launcher

Link Cast uses yt-dlp to locate public media and FFmpeg to convert it to Roku-friendly HLS.

DRM-protected, paywalled, or extraction-blocked streams are not bypassed.

Allow Dowe LanCaster through Windows Firewall on Private networks.

Sideload DoweLanCaster-Roku.zip onto the Roku before casting.
"@ | Set-Content (Join-Path $WindowsDist "START-HERE.txt")

if (Test-Path $ReleaseZip) {
    Remove-Item $ReleaseZip -Force
}

Compress-Archive -Path (Join-Path $WindowsDist "*") `
    -DestinationPath $ReleaseZip `
    -CompressionLevel Optimal

Write-Host ""
Write-Host "Release created:"
Write-Host $ReleaseZip
