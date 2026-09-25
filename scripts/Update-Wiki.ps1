param(
    [Parameter(Mandatory=$true)][string]$WikiPath,
    [Parameter(Mandatory=$true)][string]$Version,
    [string]$ReleaseUrl='',
    [string]$ReleaseDate=''
)
$ErrorActionPreference='Stop'
$ProjectRoot=Split-Path -Parent $PSScriptRoot
$ProjectFile=Join-Path $ProjectRoot 'src\DoweLanCaster.Windows\DoweLanCaster.Windows.csproj'
$ManifestFile=Join-Path $ProjectRoot 'src\DoweLanCaster.Roku\manifest'
$ReadmeFile=Join-Path $ProjectRoot 'README.md'
$ChangelogFile=Join-Path $ProjectRoot 'CHANGELOG.md'
$WikiRoot=[IO.Path]::GetFullPath($WikiPath)
if(-not (Test-Path -LiteralPath $WikiRoot -PathType Container)){throw "Wiki directory not found: $WikiRoot"}
$VersionNumber=$Version.Trim().TrimStart('v')
if($VersionNumber -notmatch '^[0-9]+\.[0-9]+\.[0-9]+(?:\.[0-9]+)?$'){throw "Invalid version '$Version'."}
$VersionTag="v$VersionNumber"
[xml]$Project=Get-Content -Raw -LiteralPath $ProjectFile
$ProjectVersion=[string]($Project.Project.PropertyGroup.Version|Select-Object -First 1)
if($ProjectVersion -ne $VersionNumber){throw "Release $VersionNumber does not match project $ProjectVersion."}
$Packages=@($Project.Project.ItemGroup.PackageReference|Where-Object Include|ForEach-Object{[pscustomobject]@{Name=[string]$_.Include;Version=[string]$_.Version}})
$Manifest=@{}
Get-Content -LiteralPath $ManifestFile|ForEach-Object{if($_ -match '^([^=]+)=(.*)$'){$Manifest[$Matches[1].Trim()]=$Matches[2].Trim()}}
$RokuVersion='{0}.{1}.{2}' -f $Manifest.major_version,$Manifest.minor_version,$Manifest.build_version
$RokuMajorMinor='{0}.{1}' -f $Manifest.major_version,$Manifest.minor_version
$ReleaseMajorMinor=($VersionNumber -split '\.' | Select-Object -First 2) -join '.'
if($RokuMajorMinor -ne $ReleaseMajorMinor){throw "Release $VersionNumber does not match Roku $RokuVersion."}
if([string]::IsNullOrWhiteSpace($ReleaseDate)){$ReleaseDate=(Get-Date).ToUniversalTime().ToString('yyyy-MM-dd')}
$Readme=Get-Content -Raw -LiteralPath $ReadmeFile
$Changelog=Get-Content -Raw -LiteralPath $ChangelogFile
$Match=[regex]::Match(
    $Changelog,
    "(?ms)^## \[$([regex]::Escape($VersionNumber))\][^\r\n]*\r?\n(?<body>.*?)(?=^## \[|\z)")
$NewIn=if($Match.Success){$Match.Groups['body'].Value.Trim()}else{
    $ReadmeMatch=[regex]::Match($Readme,"(?ms)^## New in v?$([regex]::Escape($VersionNumber))\s*\r?\n(?<body>.*?)(?=^##\s|\z)")
    if($ReadmeMatch.Success){$ReadmeMatch.Groups['body'].Value.Trim()}else{'Release details are available in the GitHub release notes.'}
}
$HasAndroidCompanion=$NewIn -match '(?i)Android companion|phone controls|APK|pairing'
$CompanionChanges=@(
    [regex]::Matches($NewIn,'(?ms)^- .+?(?=^- |^### |\z)') |
        ForEach-Object { $_.Value.Trim() } |
        Where-Object { $_ -match '(?i)Android|companion|phone|APK|pair|wireless|remote' }
) -join "`r`n"
if([string]::IsNullOrWhiteSpace($CompanionChanges)){
    $CompanionChanges='- Android companion documentation is current for this release.'
}
$ReleaseLink=if([string]::IsNullOrWhiteSpace($ReleaseUrl)){"Git tag ``$VersionTag``"}else{"[GitHub release $VersionTag]($ReleaseUrl)"}
$SnapshotStart='<!-- AUTO:RELEASE-SNAPSHOT:START -->'
$SnapshotEnd='<!-- AUTO:RELEASE-SNAPSHOT:END -->'
$Snapshot=@"
$SnapshotStart
> **Current release snapshot:** $ReleaseLink · Windows app ``$ProjectVersion`` · Roku receiver ``$RokuVersion`` · Updated $ReleaseDate UTC
$SnapshotEnd
"@.Trim()
function Set-Block([string]$Text,[string]$Start,[string]$End,[string]$Block){
    $Pattern='(?ms)^[ \t]*'+[regex]::Escape($Start)+'.*?'+[regex]::Escape($End)+'[ \t]*\r?\n?'
    if([regex]::IsMatch($Text,$Pattern)){return [regex]::Replace($Text,$Pattern,"$Block`r`n",1)}
    return $Text.TrimEnd()+"`r`n`r`n$Block`r`n"
}
function Write-Utf8([string]$Path,[string]$Content){[IO.File]::WriteAllText($Path,$Content,[Text.UTF8Encoding]::new($false))}
$CompanionPageName='Android-Companion'
if($HasAndroidCompanion -or (Test-Path -LiteralPath (Join-Path $ProjectRoot 'src\DoweLanCaster.Android'))){
    $CompanionPage=@"
# Android Companion

$Snapshot

The Dowe LanCaster Android companion lets a phone control the Dowe LanCaster application running on a Windows PC. The PC remains the host for Roku discovery, media analysis, transcoding, casting, and diagnostics; the phone provides a synchronized mobile control surface.

## Requirements

- Dowe LanCaster $VersionTag or newer running on the Windows PC.
- The Android phone and PC connected to the same trusted Wi-Fi or LAN.
- TCP port ``8770`` available for the companion service.
- UDP port ``8771`` available for automatic PC discovery.
- Android wireless debugging is required only when installing the APK wirelessly; it is not required for normal companion use.

## Pair the phone with the PC

1. Open Dowe LanCaster on the PC and select **Settings**.
2. In **Android Companion**, start the companion service if it is not already running.
3. Note the displayed PC endpoint in ``IP:PORT`` format and generate a one-time six-digit pairing code.
4. Open the Dowe LanCaster companion on the phone.
5. Wait for automatic discovery. If discovery is unavailable, enter the PC endpoint manually, such as ``10.0.0.105:8770``.
6. Enter the six-digit code and select **Pair with PC**.
7. After pairing succeeds, the phone advances to the companion controls.

Pairing codes expire and rotate after successful use. Generate a new code when pairing another phone or retrying an expired code.

## Use the companion

- **Remote:** Navigate Roku, control playback and volume, enter text, and send supported TV power commands.
- **Link Cast:** Enter a media link, analyze it on the PC, start the Roku stream, or stop it.
- **Live Cast:** Start or stop the configured desktop, window, screen, and PC-audio stream.
- **Folder Cast:** Control the PC playlist with Previous, Play, Next, and Stop.
- **TeraBox, Settings, Apps, and Diagnostics:** These sections synchronize with the corresponding PC tabs as companion controls are exposed by the desktop service.

Selecting a companion section also selects the matching tab on the PC so both interfaces remain synchronized.

## Build and install the APK

Run ``scripts\apk_installer.ps1`` from PowerShell. Its interactive menu can locate the Android SDK, clean or build the project, install the APK, pair or connect through Android wireless debugging, and create an ADB tunnel.

The Android project is located at ``src\DoweLanCaster.Android``. Debug APKs are intended for local testing. A distributable release APK should be signed with the project's release signing configuration.

## Connection troubleshooting

- Keep Dowe LanCaster open on the PC while pairing or using the companion.
- Confirm both devices are on the same non-guest network and client isolation is disabled.
- Allow Dowe LanCaster through Windows Firewall on private networks.
- If automatic discovery fails, use the exact endpoint shown by the PC.
- If the PC rejects the code, generate a new pairing code and enter all six digits before it expires.
- If controls stop responding, confirm that the selected Roku is still reachable from the PC; the phone does not connect directly to Roku.
- Android wireless-debugging pairing ports are temporary and are separate from the Dowe LanCaster companion port ``8770``.

## Security model

The companion is intended for trusted local networks. Pairing requires a one-time code, and the protocol includes request-signing and replay-protection foundations. Do not expose the companion service directly to the public Internet or forward its ports through a router.

## Changes in $VersionTag

$CompanionChanges
"@
    Write-Utf8 (Join-Path $WikiRoot "$CompanionPageName.md") $CompanionPage.TrimStart()
}
$Special=@('_Sidebar.md','_Footer.md')
Get-ChildItem -LiteralPath $WikiRoot -Filter '*.md' -File|ForEach-Object{
    if($Special -contains $_.Name -or $_.Name -like 'Release-v*.md'){return}
    $Content=Get-Content -Raw -LiteralPath $_.FullName
    $Content=Set-Block $Content $SnapshotStart $SnapshotEnd $Snapshot
    if($_.Name -eq 'Home.md'){
        $Content=[regex]::Replace($Content,'(?m)^The current release is \*\*v?[0-9]+\.[0-9]+\.[0-9]+\*\*\.',"The current release is **$VersionTag**.",1)
        $Start='<!-- AUTO:NEW-IN-RELEASE:START -->';$End='<!-- AUTO:NEW-IN-RELEASE:END -->'
        $Block=@"
$Start
## New in $VersionTag

$NewIn
$End
"@.Trim()
        $Content=Set-Block $Content $Start $End $Block
    }
    Write-Utf8 $_.FullName $Content
}
$Rows=if($Packages.Count){($Packages|ForEach-Object{"| $($_.Name) | $($_.Version) |"}) -join [Environment]::NewLine}else{'| None | — |'}
$Framework=[string]($Project.Project.PropertyGroup.TargetFramework|Select-Object -First 1)
$ReleasePage=@"
# Dowe LanCaster $VersionTag

$Snapshot

## Release information

- Release: $ReleaseLink
- Release date: $ReleaseDate UTC
- Windows application: ``$ProjectVersion``
- Roku receiver: ``$RokuVersion``
- Target framework: ``$Framework``
- Runtime package: Windows x64, self-contained

## NuGet dependencies

| Package | Version |
| --- | --- |
$Rows

## What's new

$NewIn

## Release artifacts

- ``Dowe-LanCaster-$VersionTag-Windows-x64.zip``
- ``Dowe-LanCaster-$VersionTag-Setup.exe``
- `DoweLanCaster-Roku.zip` when included by the release build

## Compatibility and limits

Dowe LanCaster requires a Windows PC and a Roku on the same trusted LAN. It does not bypass DRM, paywalls, authentication, protected playback, or site controls.
"@
Write-Utf8 (Join-Path $WikiRoot "Release-$VersionTag.md") $ReleasePage.TrimStart()
$SidebarPath=Join-Path $WikiRoot '_Sidebar.md'
$Sidebar=Get-Content -Raw -LiteralPath $SidebarPath
$Start='<!-- AUTO:LATEST-RELEASE:START -->';$End='<!-- AUTO:LATEST-RELEASE:END -->'
$Block="$Start`r`n- [Latest Release ($VersionTag)](Release-$VersionTag)`r`n$End"
$Sidebar=Set-Block $Sidebar $Start $End $Block
$CompanionStart='<!-- AUTO:ANDROID-COMPANION:START -->';$CompanionEnd='<!-- AUTO:ANDROID-COMPANION:END -->'
$CompanionBlock="$CompanionStart`r`n- [Android Companion]($CompanionPageName)`r`n$CompanionEnd"
if($HasAndroidCompanion -or (Test-Path -LiteralPath (Join-Path $WikiRoot "$CompanionPageName.md"))){
    $Sidebar=Set-Block $Sidebar $CompanionStart $CompanionEnd $CompanionBlock
}
Write-Utf8 $SidebarPath $Sidebar
$Footer="Dowe LanCaster $VersionTag · Windows-to-Roku LAN casting, streaming, voice control, and remote control · Updated $ReleaseDate UTC · Proprietary software, all rights reserved.`r`n"
Write-Utf8 (Join-Path $WikiRoot '_Footer.md') $Footer
$Pages=Get-ChildItem -LiteralPath $WikiRoot -Filter '*.md' -File
$Names=@{};$Pages|ForEach-Object{$Names[$_.BaseName]=$true}
$Broken=@()
$Pages|ForEach-Object{$Page=$_;$Content=Get-Content -Raw -LiteralPath $_.FullName;[regex]::Matches($Content,'\[[^\]]+\]\(([^)]+)\)')|ForEach-Object{$Target=$_.Groups[1].Value;if($Target -notmatch '^(https?://|#)' -and -not $Names.ContainsKey($Target)){$Broken+="$($Page.Name) -> $Target"}}}
if($Broken.Count){throw "Broken wiki links:`n$($Broken -join "`n")"}
Write-Host "Wiki updated for $VersionTag ($($Pages.Count) pages)."
