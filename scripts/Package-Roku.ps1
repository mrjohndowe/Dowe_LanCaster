$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$RokuFolder = Join-Path $ProjectRoot "src\DoweLanCaster.Roku"
$DistFolder = Join-Path $ProjectRoot "dist"
$OutputZip = Join-Path $DistFolder "DoweLanCaster-Roku.zip"

New-Item -ItemType Directory -Force -Path $DistFolder | Out-Null

if (Test-Path $OutputZip) {
    Remove-Item $OutputZip -Force
}

Push-Location $RokuFolder
try {
    $items = @("manifest", "source", "components")

    if (Test-Path ".\images") {
        $items += "images"
    }

    if (Test-Path ".\videos") {
        $items += "videos"
    }

    & tar.exe -a -c -f $OutputZip @items

    if ($LASTEXITCODE -ne 0) {
        throw "tar.exe failed with exit code $LASTEXITCODE"
    }
}
finally {
    Pop-Location
}

Write-Host "Created: $OutputZip"
Write-Host ""
Write-Host "Package contents:"
& tar.exe -tf $OutputZip
