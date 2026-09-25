[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [ValidateSet("BuildAndInstall", "Build", "Clean", "RebuildAndInstall", "Connect", "Tunnel", "UninstallFirst")]
    [string]$Action,

    [string]$ProjectPath,

    [string]$DeviceSerial,

    [string]$WirelessAddress,

    [string]$PairingAddress,

    [ValidateRange(1, 65535)]
    [int]$TunnelPort = 8770,

    [switch]$NoMenuReturn,

    # Retained for compatibility. Equivalent to -Action RebuildAndInstall.
    [switch]$Clean
)

clear
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot

if ($Clean -and $Action) {
    throw "Do not combine the legacy -Clean switch with -Action. Use -Action RebuildAndInstall instead."
}

function Read-MenuChoice {
    param(
        [Parameter(Mandatory)]
        [string]$Prompt,

        [Parameter(Mandatory)]
        [string[]]$ValidChoices
    )

    while ($true) {
        $response = Read-Host $Prompt
        if ($null -eq $response) {
            throw "Input was cancelled."
        }
        $choice = $response.Trim()
        if ($choice -in $ValidChoices) {
            return $choice
        }
        Write-Host "Please choose one of: $($ValidChoices -join ', ')." -ForegroundColor Yellow
    }
}

function Read-RequiredInput {
    param(
        [Parameter(Mandatory)]
        [string]$Prompt
    )

    while ($true) {
        $response = Read-Host $Prompt
        if ($null -eq $response) {
            throw "Input was cancelled."
        }
        $value = $response.Trim()
        if (-not [string]::IsNullOrWhiteSpace($value)) {
            return $value
        }
        Write-Host "A value is required." -ForegroundColor Yellow
    }
}

function Read-InputWithDefault {
    param(
        [Parameter(Mandatory)]
        [string]$Prompt,

        [Parameter(Mandatory)]
        [string]$DefaultValue
    )

    $response = Read-Host "$Prompt [$DefaultValue]"
    if ($null -eq $response -or [string]::IsNullOrWhiteSpace($response)) {
        return $DefaultValue
    }
    return $response.Trim()
}

function Read-AdbNetworkAddress {
    param(
        [Parameter(Mandatory)]
        [string]$Label,

        [string]$DefaultAddress
    )

    $defaultHost = $null
    $defaultPort = $null
    if ($DefaultAddress -and
        $DefaultAddress -match "^(?<host>\[[^\]]+\]|[^:\s]+):(?<port>[1-9][0-9]{0,4})$") {
        $defaultHost = $Matches.host
        $defaultPort = $Matches.port
    }

    if ($defaultHost) {
        $hostAddress = Read-InputWithDefault -Prompt "$Label IP address" -DefaultValue $defaultHost
    }
    else {
        $hostAddress = Read-RequiredInput -Prompt "$Label IP address"
    }

    while ($true) {
        if ($defaultPort) {
            $portText = Read-InputWithDefault -Prompt "$Label port" -DefaultValue $defaultPort
        }
        else {
            $portText = Read-RequiredInput -Prompt "$Label port"
        }

        $port = 0
        if ([int]::TryParse($portText, [ref]$port) -and $port -ge 1 -and $port -le 65535) {
            break
        }
        Write-Host "Enter a port from 1 through 65535." -ForegroundColor Yellow
    }

    return "${hostAddress}:$port"
}

function createBanner {
    [CmdletBinding()]
    param(
        [ConsoleColor[]]$Palette = @(
            [ConsoleColor]::DarkCyan
            [ConsoleColor]::Cyan
            [ConsoleColor]::Blue
            [ConsoleColor]::DarkBlue
            [ConsoleColor]::White
            [ConsoleColor]::Red
        ),

        [ConsoleColor]$BorderColor = [ConsoleColor]::Cyan
    )

    $banner = @'
_(`-')                  .->    (`-')  _             (`-')  _ <-. (`-')_         (`-')  _  (`-').->`-')      (`-')  _   (`-')
( (OO ).->    .->    (`(`-')/`) ( OO).-/       <-.   (OO ).-/    \( OO) )        (OO ).-/  ( OO)_ ( OO).->   ( OO).-/<-.(OO )
\    .'_(`-')----. ,-`( OO).',(,------.     ,--. )  / ,---.  ,--./ ,--/\-,-----./ ,---.  (_)--\_)/    '._  (,------.,------,)
'`'-..__| OO).-.  '|  |\  |  | |  .---'     |  (`-')| \ /`.\ |   \ |  | |  .--./| \ /`.\ /    _ /|'--...__) |  .---'|   /`. '
|  |  ' ( _) | |  ||  | '.|  |(|  '--.      |  |OO )'-'|_.' ||  . '|  |)_) (`-')'-'|_.' |\_..`--.`--.  .--'(|  '--. |  |_.' |
|  |  / :\|  |)|  ||  |.'.|  | |  .--'     (|  '__ (|  .-.  ||  |\    |||  |OO ||  .-.  |.-._)   \  |  |    |  .--' |  .   .'
|  '-'  / '  '-'  '|   ,'.   | |  `---.     |     |'|  | |  ||  | \   (_'  '--'\|  | |  |\       /  |  |    |  `---.|  |\  \
`------'   `-----' `--'   '--' `------'     `-----' `--' `--'`--'  `--'  `-----'`--' `--' `-----'   `--'    `------'`--' '--'
'@ -split "`r?`n"

    if ($Palette.Count -eq 0) {
        $Palette = @([ConsoleColor]::Cyan)
    }

    $bannerWidth = ($banner | Measure-Object -Property Length -Maximum).Maximum
    $colorBandWidth = [Math]::Max(1, [Math]::Ceiling($bannerWidth / $Palette.Count))

    Write-Host ('=' * $bannerWidth) -ForegroundColor $BorderColor
    Write-Host ""
    foreach ($line in $banner) {
        for ($position = 0; $position -lt $line.Length; $position++) {
            $character = $line[$position]
            if ([char]::IsWhiteSpace($character)) {
                Write-Host $character -NoNewline
                continue
            }

            $paletteIndex = [Math]::Min(
                [Math]::Floor($position / $colorBandWidth),
                $Palette.Count - 1
            )
            Write-Host $character -NoNewline -ForegroundColor $Palette[$paletteIndex]
        }
        Write-Host ""
    }

    Write-Host ('=' * $bannerWidth) -ForegroundColor $BorderColor
    $menuTitle = " Installation Menu "
    $titlePadding = [Math]::Max(0, $bannerWidth - $menuTitle.Length)

    $leftPadding = [Math]::Floor($titlePadding / 2)
    $rightPadding = $titlePadding - $leftPadding

    Write-Color (
        ('=' * $leftPadding) +
        $menuTitle +
        ('=' * $rightPadding)
    ) Green Black 1



    [Console]::ResetColor()
    Write-Host ""
}

$interactiveMenu = -not $Clean -and -not $Action
$returnToMenu = -not $NoMenuReturn

function Write-Color {
    param(
        [Parameter(Mandatory)]
        [string]$Message,

        [Parameter(Mandatory)]
        [ValidateSet("Black", "DarkBlue", "DarkGreen", "DarkCyan", "DarkRed", "DarkMagenta", "DarkYellow", "Gray", "Blue", "Green", "Cyan", "Red", "Magenta", "Yellow", "White")]
        [ConsoleCOlor]$ForegroundColor = "DarkGreen",

        [Parameter(Mandatory)]
        [ValidateSet("Black", "DarkBlue", "DarkGreen", "DarkCyan", "DarkRed", "DarkMagenta", "DarkYellow", "Gray", "Blue", "Green", "Cyan", "Red", "Magenta", "Yellow", "White")]
        [ConsoleColor]$backgroundColor = "Black",

        [boolean]$upperCase
    )

    if ($upperCase) {
        Write-Host $Message.ToUpper() `
            -ForegroundColor $ForegroundColor `
            -BackgroundColor $backgroundColor
        [Console]::ResetColor()
    }
    else {
        Write-Host $Message `
            -ForegroundColor $ForegroundColor `
            -BackgroundColor $backgroundColor
        [Console]::ResetColor()
    }
}

function Return-ToMainMenu {
    if (-not $returnToMenu) {
        return
    }

    Write-Host ""
    Read-Host "Press Enter to return to the main menu" | Out-Null
    & $PSCommandPath
    exit $LASTEXITCODE
}

trap {
    $failureMessage = if ($_.Exception.Message) { $_.Exception.Message } else { $_.ToString() }
    [Console]::Error.WriteLine("")
    [Console]::Error.WriteLine("Operation failed: $failureMessage")
    if ($returnToMenu) {
        Return-ToMainMenu
    }
    exit 1
}

if ($Clean) {
    $effectiveAction = "RebuildAndInstall"
}
elseif ($Action) {
    $effectiveAction = $Action
}
else {
    Write-Host ""
    createBanner


    # Write-Host ""
    # Write-Host "=========================================" -ForegroundColor Cyan
    # Write-Host " Dowe LanCaster Android Companion" -ForegroundColor Cyan
    # Write-Host "=========================================" -ForegroundColor Cyan
    Write-Color "1. Build APK only" White Black 1
    Write-Color "2. Clean build files only" White Black 1
    Write-Color "3. Build APK and install" White Black 1
    Write-Color "4. Clean, rebuild, and install" White Black 1
    Write-Color "5. Connect to phone" White Black 1
    Write-Color "6. Create PC service tunnel" White Black 1
    Write-Color "7. Uninstall, Clean, Rebuild, and Re-Install" White Black 1
    Write-Color "8. Exit" Red Black 1
    Write-Host ""

    $menuChoice = Read-MenuChoice -Prompt "Select an option [1-8]" -ValidChoices @("1", "2", "3", "4", "5", "6", "7", "8")
    $effectiveAction = switch ($menuChoice) {
        "1" { "Build" }
        "2" { "Clean" }
        "3" { "BuildAndInstall" }
        "4" { "RebuildAndInstall" }
        "5" { "Connect" }
        "6" { "Tunnel" }
        "7" { "UninstallFirst" }
        "8" { Write-Host "No changes were made."; Return-ToMainMenu; exit 0 }
    }
}

$needsBuild = $effectiveAction -in @("BuildAndInstall", "Build", "Clean", "RebuildAndInstall", "UninstallFirst")
if ($interactiveMenu -and $needsBuild -and $effectiveAction -ne "Clean" -and
    -not $PSBoundParameters.ContainsKey("Configuration")) {
    Write-Host ""
    Write-Color "1. Debug (recommended for testing)" White Black 1
    Write-Color "2. Release" White Black 1
    $configurationChoice = Read-MenuChoice -Prompt "Select build type [1-2]" -ValidChoices @("1", "2")
    $Configuration = if ($configurationChoice -eq "2") { "Release" } else { "Debug" }
}

$willUseDevice = $effectiveAction -in @("BuildAndInstall", "RebuildAndInstall", "UninstallFirst", "Connect", "Tunnel")
if ($interactiveMenu -and $willUseDevice -and -not $DeviceSerial -and -not $WirelessAddress) {
    Write-Host ""
    Write-Color "How should the phone be connected?" White Blue 1
    Write-Color "1. Use a device already connected to ADB" White Black 1
    Write-Color "2. Connect using Wireless Debugging" White Black 1
    Write-Color "3. Pair a new Wireless Debugging device, then connect" White Black 1
    Write-Color "4. Cancel" Red Black 1
    $connectionChoice = Read-MenuChoice -Prompt "Select connection type [1-4]" -ValidChoices @("1", "2", "3", "4")

    switch ($connectionChoice) {
        "1" { }
        "2" {
            $WirelessAddress = Read-AdbNetworkAddress -Label "Wireless Debugging"
        }
        "3" {
            $PairingAddress = Read-AdbNetworkAddress -Label "Pairing"
        }
        "4" { Write-Host "Operation cancelled."; Return-ToMainMenu; exit 0 }
    }
}



$gradleWrapper = $null
if ($needsBuild) {
    if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
        $ProjectPath = Join-Path $repositoryRoot "src\DoweLanCaster.Android"
    }
    elseif (-not [System.IO.Path]::IsPathRooted($ProjectPath)) {
        $ProjectPath = Join-Path $repositoryRoot $ProjectPath
    }

    if (-not (Test-Path -LiteralPath $ProjectPath -PathType Container)) {
        if ($interactiveMenu) {
            Write-Host "The Android project was not found at the expected location." -ForegroundColor Yellow
            $enteredProjectPath = Read-RequiredInput -Prompt "Enter the Android Gradle project directory"
            if (-not [System.IO.Path]::IsPathRooted($enteredProjectPath)) {
                $enteredProjectPath = Join-Path $repositoryRoot $enteredProjectPath
            }
            $ProjectPath = $enteredProjectPath
        }

        if (-not (Test-Path -LiteralPath $ProjectPath -PathType Container)) {
            throw "Android companion project not found at '$ProjectPath'."
        }
    }

    $gradleWrapper = Join-Path $ProjectPath "gradlew.bat"
    if (-not (Test-Path -LiteralPath $gradleWrapper -PathType Leaf)) {
        throw "Gradle wrapper not found at '$gradleWrapper'. The Android project must include gradlew.bat."
    }

    if (-not (Get-Command java.exe -ErrorAction SilentlyContinue)) {
        if ($interactiveMenu) {
            $enteredJavaHome = Read-RequiredInput -Prompt "Java was not found. Enter the JDK 17 or newer installation directory"
            $javaExecutable = Join-Path $enteredJavaHome "bin\java.exe"
            if (Test-Path -LiteralPath $javaExecutable -PathType Leaf) {
                $env:JAVA_HOME = $enteredJavaHome
                $env:Path = "$(Join-Path $enteredJavaHome 'bin');$env:Path"
            }
        }

        if (-not (Get-Command java.exe -ErrorAction SilentlyContinue)) {
            throw "Java was not found. Install JDK 17 or newer and set JAVA_HOME."
        }
    }
}

$sdkCandidates = @(
    $env:ANDROID_SDK_ROOT,
    $env:ANDROID_HOME,
    $(if ($env:LOCALAPPDATA) { Join-Path $env:LOCALAPPDATA "Android\Sdk" })
) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique

$androidSdk = $sdkCandidates |
Where-Object { Test-Path -LiteralPath (Join-Path $_ "platform-tools\adb.exe") -PathType Leaf } |
Select-Object -First 1

if (-not $androidSdk) {
    if ($interactiveMenu) {
        $enteredSdk = Read-RequiredInput -Prompt "Android SDK not found. Enter the Android SDK directory"
        if (Test-Path -LiteralPath (Join-Path $enteredSdk "platform-tools\adb.exe") -PathType Leaf) {
            $androidSdk = $enteredSdk
        }
    }

    if (-not $androidSdk) {
        throw "Android SDK platform-tools were not found. Install platform-tools with Android Studio."
    }
}

$androidSdk = (Resolve-Path -LiteralPath $androidSdk).Path
$env:ANDROID_HOME = $androidSdk
$env:ANDROID_SDK_ROOT = $androidSdk

$adb = Join-Path $androidSdk "platform-tools\adb.exe"


function Assert-AdbNetworkAddress {
    param(
        [Parameter(Mandatory)]
        [string]$Address,

        [Parameter(Mandatory)]
        [string]$ParameterName
    )

    if ($Address -notmatch "^(\[[^\]]+\]|[^:\s]+):(?<port>[1-9][0-9]{0,4})$") {
        throw "$ParameterName must use the address shown by Android Wireless Debugging, such as 192.168.1.25:37145."
    }

    if ([int]$Matches.port -gt 65535) {
        throw "$ParameterName contains an invalid TCP port."
    }
}

function Get-HostFromAdbAddress {
    param(
        [Parameter(Mandatory)]
        [string]$Address
    )

    if ($Address -match "^(?<host>\[[^\]]+\]|[^:\s]+):[1-9][0-9]{0,4}$") {
        return $Matches.host
    }
    throw "Could not read the IP address from '$Address'."
}

function Read-AdbPort {
    param(
        [Parameter(Mandatory)]
        [string]$Label
    )

    while ($true) {
        $portText = Read-RequiredInput -Prompt "$Label port"
        $port = 0
        if ([int]::TryParse($portText, [ref]$port) -and $port -ge 1 -and $port -le 65535) {
            return $port
        }
        Write-Host "Enter a port from 1 through 65535." -ForegroundColor Yellow
    }
}

function Find-AdbWirelessConnectionAddress {
    param(
        [Parameter(Mandatory)]
        [string]$ExpectedHost
    )

    for ($attempt = 1; $attempt -le 5; $attempt++) {
        $services = @(& $adb mdns services 2>&1)
        foreach ($service in $services) {
            $line = $service.ToString()
            if ($line -match "_adb-tls-connect\._tcp" -and
                $line -match "(?<address>(?:[0-9]{1,3}\.){3}[0-9]{1,3}):(?<port>[1-9][0-9]{0,4})") {
                $discoveredAddress = $Matches.address
                if ($discoveredAddress -like "$ExpectedHost`:*") {
                    return $discoveredAddress
                }
            }
        }
        if ($attempt -lt 5) {
            Start-Sleep -Seconds 1
        }
    }
    return $null
}

if ($DeviceSerial -and $WirelessAddress -and $DeviceSerial -ne $WirelessAddress) {
    throw "Use either -DeviceSerial for an existing ADB connection or -WirelessAddress for a new wireless connection, not both."
}

if ($WirelessAddress) {
    Assert-AdbNetworkAddress -Address $WirelessAddress -ParameterName "-WirelessAddress"
}

if ($PairingAddress) {
    Assert-AdbNetworkAddress -Address $PairingAddress -ParameterName "-PairingAddress"
}

function Invoke-CheckedCommand {
    param(
        [Parameter(Mandatory)]
        [string]$Command,

        [Parameter(Mandatory)]
        [string[]]$Arguments,

        [Parameter(Mandatory)]
        [string]$FailureMessage
    )

    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$FailureMessage Exit code: $LASTEXITCODE."
    }
}


Write-Host ""
Write-Host "Dowe LanCaster Android Companion: $effectiveAction ($Configuration)"
Write-Host "Android SDK: $androidSdk"
if ($needsBuild) {
    Push-Location $ProjectPath
    try {


        if ($effectiveAction -in @("Clean", "RebuildAndInstall")) {
            Write-Host "Cleaning Android build outputs..."
            Invoke-CheckedCommand -Command $gradleWrapper -Arguments @("clean", "--console=plain", "--no-daemon") `
                -FailureMessage "Gradle clean failed."
        }

        if ($effectiveAction -ne "Clean") {
            Write-Host "Building the $Configuration APK..."
            $assembleTask = "assemble$Configuration"
            Invoke-CheckedCommand -Command $gradleWrapper -Arguments @($assembleTask, "--console=plain", "--no-daemon") `
                -FailureMessage "Android companion build failed."
        }
    }
    finally {
        Pop-Location
    }

    if ($effectiveAction -eq "Clean") {
        Write-Host ""
        Write-Host "Android build outputs cleaned successfully."
        Return-ToMainMenu
        exit 0
    }

    $variantDirectory = $Configuration.ToLowerInvariant()
    $apkDirectory = Join-Path $ProjectPath "app\build\outputs\apk\$variantDirectory"
    $apk = Get-ChildItem -LiteralPath $apkDirectory -Filter "*.apk" -File -Recurse -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -notmatch "androidTest|unaligned" } |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 1

    if (-not $apk) {
        throw "Gradle completed, but no $Configuration APK was found under '$apkDirectory'."
    }

    if ($apk.Name -match "unsigned") {
        throw "The generated APK '$($apk.FullName)' is unsigned. Configure Android release signing or build the Debug variant."
    }

    Write-Host ""
    Write-Host "APK built successfully."
    Write-Host "APK: $($apk.FullName)"

    if ($effectiveAction -eq "Build") {
        Return-ToMainMenu
        exit 0
    }
}

Invoke-CheckedCommand -Command $adb -Arguments @("start-server") `
    -FailureMessage "ADB could not start."

if ($PairingAddress) {
    Write-Host ""
    Write-Host "Pairing with Android at $PairingAddress..."
    Write-Host "Enter the six-digit pairing code displayed on the phone when ADB asks for it."
    Invoke-CheckedCommand -Command $adb -Arguments @("pair", $PairingAddress) `
        -FailureMessage "Wireless debugging pairing failed. Check the pairing address, code, and Wi-Fi network."

    if (-not $WirelessAddress) {
        $pairedHost = Get-HostFromAdbAddress -Address $PairingAddress
        Write-Host "Discovering the phone's Wireless Debugging connection port..."
        $WirelessAddress = Find-AdbWirelessConnectionAddress -ExpectedHost $pairedHost
        if ($WirelessAddress) {
            Write-Host "Discovered Wireless Debugging address: $WirelessAddress"
        }
        elseif (-not [Console]::IsInputRedirected) {
            Write-Host "Automatic discovery did not return the connection port." -ForegroundColor Yellow
            Write-Host "On the main Wireless debugging screen, find 'IP address & Port'."
            $connectionPort = Read-AdbPort -Label "Main Wireless Debugging"
            $WirelessAddress = "${pairedHost}:$connectionPort"
        }
        else {
            throw "Pairing succeeded, but the Wireless Debugging connection port could not be discovered."
        }
    }
}

if ($WirelessAddress) {
    Write-Host ""
    Write-Host "Connecting to Android at $WirelessAddress..."

    # Remove a stale/offline transport before attempting a fresh wireless connection.
    & $adb disconnect $WirelessAddress 2>&1 | ForEach-Object { Write-Host $_ }
    $connectOutput = @(& $adb connect $WirelessAddress 2>&1)
    $connectOutput | ForEach-Object { Write-Host $_ }
    $connectSucceeded = $LASTEXITCODE -eq 0 -and
    ($connectOutput -join "`n") -match "(?i)(connected to|already connected to)"

    if (-not $connectSucceeded -and -not $PairingAddress -and -not [Console]::IsInputRedirected) {
        Write-Host ""
        Write-Host "The phone did not accept the connection." -ForegroundColor Yellow
        Write-Host "If the phone is showing 'Pair device with pairing code', the address you entered is a pairing address, not the main connection address."
        Write-Host "1. Pair the phone now"
        Write-Host "2. Enter a different Wireless Debugging connection address"
        Write-Host "3. Cancel"
        $retryChoice = Read-MenuChoice -Prompt "Select an option [1-3]" -ValidChoices @("1", "2", "3")

        if ($retryChoice -eq "1") {
            $PairingAddress = Read-AdbNetworkAddress `
                -Label "Pairing" `
                -DefaultAddress $WirelessAddress
            Assert-AdbNetworkAddress -Address $PairingAddress -ParameterName "Pairing address"

            Write-Host ""
            Write-Host "Pairing with Android at $PairingAddress..."
            Write-Host "Enter the six-digit pairing code displayed on the phone when ADB asks for it."
            Invoke-CheckedCommand -Command $adb -Arguments @("pair", $PairingAddress) `
                -FailureMessage "Wireless debugging pairing failed. Check the pairing address, code, and Wi-Fi network."

            $pairedHost = Get-HostFromAdbAddress -Address $PairingAddress
            Write-Host "Discovering the phone's Wireless Debugging connection port..."
            $WirelessAddress = Find-AdbWirelessConnectionAddress -ExpectedHost $pairedHost
            if ($WirelessAddress) {
                Write-Host "Discovered Wireless Debugging address: $WirelessAddress"
            }
            else {
                Write-Host "Automatic discovery did not return the connection port." -ForegroundColor Yellow
                Write-Host "On the main Wireless debugging screen, find 'IP address & Port'."
                $connectionPort = Read-AdbPort -Label "Main Wireless Debugging"
                $WirelessAddress = "${pairedHost}:$connectionPort"
            }
        }
        elseif ($retryChoice -eq "2") {
            $WirelessAddress = Read-AdbNetworkAddress -Label "Main Wireless Debugging"
            Assert-AdbNetworkAddress -Address $WirelessAddress -ParameterName "Wireless Debugging address"
        }
        else {
            Write-Host "Connection cancelled."
            Return-ToMainMenu
            exit 0
        }

        Write-Host ""
        Write-Host "Connecting to Android at $WirelessAddress..."
        & $adb disconnect $WirelessAddress 2>&1 | ForEach-Object { Write-Host $_ }
        $connectOutput = @(& $adb connect $WirelessAddress 2>&1)
        $connectOutput | ForEach-Object { Write-Host $_ }
        $connectSucceeded = $LASTEXITCODE -eq 0 -and
        ($connectOutput -join "`n") -match "(?i)(connected to|already connected to)"
    }

    if (-not $connectSucceeded) {
        throw "Wireless debugging connection failed. Use the main Wireless Debugging IP address and port, not the temporary pairing port."
    }

    $DeviceSerial = $WirelessAddress
}

$connectedDevices = @(& $adb devices) |
ForEach-Object {
    if ($_ -match "^(?<serial>\S+)\s+(?<state>device|unauthorized|offline)$") {
        [pscustomobject]@{ Serial = $Matches.serial; State = $Matches.state }
    }
}

if ($DeviceSerial) {
    $selectedDevice = $connectedDevices | Where-Object Serial -EQ $DeviceSerial | Select-Object -First 1
    if (-not $selectedDevice) {
        throw "Android device '$DeviceSerial' was not reported by ADB. Run 'adb devices' and check the serial."
    }
}
else {
    $readyDevices = @($connectedDevices | Where-Object State -EQ "device")
    $blockedDevices = @($connectedDevices | Where-Object State -NE "device")

    if ($readyDevices.Count -eq 0) {
        if ($blockedDevices.Count -gt 0) {
            $blockedSummary = ($blockedDevices | ForEach-Object { "$($_.Serial) ($($_.State))" }) -join ", "
            throw "No authorized Android device is available. Unlock the device and approve USB debugging. ADB reported: $blockedSummary."
        }
        throw "No Android device is connected. Enable USB debugging, connect the phone, and approve this PC."
    }

    if ($readyDevices.Count -gt 1) {
        $serials = ($readyDevices | ForEach-Object Serial) -join ", "
        if (-not $interactiveMenu) {
            throw "More than one Android device is connected: $serials. Run this script again with -DeviceSerial <serial>."
        }

        Write-Host ""
        Write-Host "Choose the Android device:"
        for ($index = 0; $index -lt $readyDevices.Count; $index++) {
            Write-Host "$($index + 1). $($readyDevices[$index].Serial)"
        }
        $validDeviceChoices = 1..$readyDevices.Count | ForEach-Object { $_.ToString() }
        $deviceChoice = Read-MenuChoice -Prompt "Select a device" -ValidChoices $validDeviceChoices
        $selectedDevice = $readyDevices[[int]$deviceChoice - 1]
    }
    else {
        $selectedDevice = $readyDevices[0]
    }
}

if ($selectedDevice.State -ne "device") {
    throw "Android device '$($selectedDevice.Serial)' is $($selectedDevice.State). Unlock it and approve USB debugging."
}

if ($effectiveAction -eq "UninstallFirst") {
    Push-Location $ProjectPath
    $packageId = "com.dowelancaster.companion"
    Write-Host "Uninstalling the old app..."

    & $adb -s $selectedDevice.Serial uninstall $packageId
    if ($LASTEXITCODE -ne 0) {
        Write-Host "The app was not already installed. Continuing:..." -ForegroundColor White -BackgroundColor Red
    }
    Write-Host "Cleaning old build output..."

    Invoke-CheckedCommand -Command $gradleWrapper -Arguments @(
        "clean",
        "--console=plain",
        "--no-daemon"
    ) -FailureMessage "Gradle clean failed."

    Write-Host "Building the new $Configuration APK..."

    Invoke-CheckedCommand -Command $gradleWrapper -Arguments @(
        "assemble$Configuration",
        "--console=plain",
        "--no-daemon"
    ) -FailureMessage "APK build failed."

    $variantDirectory = $Configuration.ToLowerInvariant()
    # $apkDirectory = "C:\Users\MrJohnDowe\AppData\Local\Android\Sdk\platform-tools\adb.exe"
    $apkDirectory = Join-Path $ProjectPath "app\build\outputs\apk\$variantDirectory"

    $apk = Get-ChildItem -LiteralPath $apkDirectory -Filter "*.apk" -File -Recurse |
    Where-Object { $_.Name -notmatch "androidTest|unaligned|unsigned" } |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 1

    if (-not $apk) {
        throw "No $Configuration APK was found after build."
    }

    Write-Host "Installing the new APK..."

    Invoke-CheckedCommand -Command $adb -Arguments @(
        "-s",
        $selectedDevice.Serial,
        "install",
        "-r",
        $apk.FullName
    ) -FailureMessage "APK installation failed"

    Write-Host "Finished: uninstalled, cleaned, rebuilt, and reinstalled." -ForegroundColor Green
    Return-ToMainMenu



}

if ($effectiveAction -eq "Connect") {
    Write-Host ""
    Write-Host "Phone connected successfully."
    Write-Host "Device: $($selectedDevice.Serial)"
    Return-ToMainMenu
    exit 0
}

if ($effectiveAction -eq "Tunnel") {
    Write-Host ""
    Write-Host "Creating reverse tunnel for the Dowe LanCaster PC service..."
    Invoke-CheckedCommand -Command $adb -Arguments @(
        "-s", $selectedDevice.Serial,
        "reverse", "tcp:$TunnelPort", "tcp:$TunnelPort"
    ) -FailureMessage "ADB reverse tunnel creation failed."

    Write-Host ""
    Write-Host "PC service tunnel created successfully."
    Write-Host "Device: $($selectedDevice.Serial)"
    Write-Host "Phone endpoint: https://127.0.0.1:$TunnelPort"
    Write-Host "PC destination: 127.0.0.1:$TunnelPort"
    Return-ToMainMenu
    exit 0
}

Write-Host ""
Write-Host "Installing APK on $($selectedDevice.Serial)..."
Invoke-CheckedCommand -Command $adb -Arguments @(
    "-s", $selectedDevice.Serial,
    "install", "-r", $apk.FullName
) -FailureMessage "APK installation failed."

Write-Host ""
Write-Host "Android companion installed successfully."
Write-Host "Device: $($selectedDevice.Serial)"
Write-Host "APK: $($apk.FullName)"
Return-ToMainMenu
