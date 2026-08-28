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
$WikiRoot=[IO.Path]::GetFullPath($WikiPath)
if(-not (Test-Path -LiteralPath $WikiRoot -PathType Container)){throw "Wiki directory not found: $WikiRoot"}
$VersionNumber=$Version.Trim().TrimStart('v')
if($VersionNumber -notmatch '^[0-9]+\.[0-9]+\.[0-9]+$'){throw "Invalid version '$Version'."}
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
$Match=[regex]::Match($Readme,"(?ms)^## New in v?$([regex]::Escape($VersionNumber))\s*\r?\n(?<body>.*?)(?=^##\s|\z)")
$NewIn=if($Match.Success){$Match.Groups['body'].Value.Trim()}else{'Release details are available in the GitHub release notes.'}
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
Write-Utf8 $SidebarPath (Set-Block $Sidebar $Start $End $Block)
$Footer="Dowe LanCaster $VersionTag · Windows-to-Roku LAN casting, streaming, voice control, and remote control · Updated $ReleaseDate UTC · Proprietary software, all rights reserved.`r`n"
Write-Utf8 (Join-Path $WikiRoot '_Footer.md') $Footer
$Pages=Get-ChildItem -LiteralPath $WikiRoot -Filter '*.md' -File
$Names=@{};$Pages|ForEach-Object{$Names[$_.BaseName]=$true}
$Broken=@()
$Pages|ForEach-Object{$Page=$_;$Content=Get-Content -Raw -LiteralPath $_.FullName;[regex]::Matches($Content,'\[[^\]]+\]\(([^)]+)\)')|ForEach-Object{$Target=$_.Groups[1].Value;if($Target -notmatch '^(https?://|#)' -and -not $Names.ContainsKey($Target)){$Broken+="$($Page.Name) -> $Target"}}}
if($Broken.Count){throw "Broken wiki links:`n$($Broken -join "`n")"}
Write-Host "Wiki updated for $VersionTag ($($Pages.Count) pages)."
