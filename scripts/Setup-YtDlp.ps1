$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$ToolsDir = Join-Path $ProjectRoot "tools\yt-dlp"
$Exe = Join-Path $ToolsDir "yt-dlp.exe"

New-Item -ItemType Directory -Force -Path $ToolsDir | Out-Null

if (Test-Path $Exe) {
    Write-Host "Using the bundled yt-dlp executable:"
    Write-Host $Exe
    exit 0
}

$Url = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe"

Write-Host "Downloading yt-dlp from the official GitHub release..."
$Download = "$Exe.download"
$Headers = @{ "User-Agent" = "Dowe-LanCaster-Dependency-Setup" }
$Attempts = 4

for ($Attempt = 1; $Attempt -le $Attempts; $Attempt++) {
    try {
        Invoke-WebRequest -Uri $Url -OutFile $Download -UseBasicParsing -Headers $Headers
        Move-Item -LiteralPath $Download -Destination $Exe -Force
        break
    }
    catch {
        if (Test-Path -LiteralPath $Download) {
            Remove-Item -LiteralPath $Download -Force
        }

        if ($Attempt -eq $Attempts) {
            throw
        }

        $Delay = [Math]::Pow(2, $Attempt)
        Write-Warning "yt-dlp download attempt $Attempt failed. Retrying in $Delay seconds."
        Start-Sleep -Seconds $Delay
    }
}

Write-Host ""
Write-Host "yt-dlp installed:"
Write-Host $Exe
& $Exe --version
