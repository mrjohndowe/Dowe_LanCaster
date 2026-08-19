# Development

## Prerequisites

- Windows
- .NET 8 SDK
- PowerShell
- FFmpeg and ffprobe
- yt-dlp for Link Cast webpage extraction
- A Roku with developer mode enabled for end-to-end receiver testing

## Repository layout

| Path | Purpose |
| --- | --- |
| `src\DoweLanCaster.Windows` | WPF Windows application and services. |
| `src\DoweLanCaster.Roku` | Roku SceneGraph receiver source and manifest. |
| `scripts` | Dependency setup, Roku packaging, checks, and release automation. |
| `docs` | Historical phase notes and feature-specific documentation. |
| `dist` | Generated Windows and Roku packages. |
| `tools\ffmpeg` | Local FFmpeg binaries. |
| `tools\yt-dlp` | Local yt-dlp binary. |

## Build locally

```powershell
.\SETUP-DEPENDENCIES.cmd
dotnet restore
dotnet build
```

If a running application locks build output, close it or stop the `DoweLanCaster` process before rebuilding.

## Package the Roku channel

```powershell
.\scripts\Package-Roku.ps1
```

The script creates `dist\DoweLanCaster-Roku.zip` while preserving the Roku `source` and `components` directory structure.

## Testing checklist

- Discovery: automatic scan and manual IP connection.
- Remote: navigation, playback keys, text entry, installed-app listing, and app launch.
- File Cast: a known Roku-compatible local MP4.
- Live Cast: desktop/monitor/window, both silent and with system audio.
- Link Cast: a direct URL and a public webpage URL.
- Folder Cast: mixed formats, auto-next, repeat modes, shuffle, and a deliberately invalid item to test skipping.
- Networking: clean failure messages when firewall or ports are unavailable.
- Encoders: CPU baseline, then each detected hardware encoder.
- Roku: current receiver package sideloaded after receiver changes.

## Continuous integration

The GitHub Actions workflow runs on Windows for pushes to `main`/`master`, pull requests, and manual dispatch. It installs .NET 8, prepares dependencies, builds the release, and uploads Windows and Roku artifacts.

## Licensing

This repository is proprietary and all rights are reserved. The license prohibits copying, modification, distribution, sublicensing, reverse engineering, and derivative works without prior written permission from the copyright holder. Confirm authorization before contributing or redistributing builds.

