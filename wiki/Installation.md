# Installation

## Requirements

- A 64-bit Windows PC
- A Roku device or Roku TV on the same LAN as the PC
- Windows Firewall access on private networks
- For source builds: the .NET 8 SDK
- Roku developer mode for sideloading the receiver channel

FFmpeg, ffprobe, and yt-dlp are required for transcoded and link-based workflows. The release build includes these tools; a source checkout can install them with the included setup script.

## Install from a release package

1. Download and extract `DoweLanCaster-Windows-x64.zip`.
2. Keep the extracted folder together; do not move only `DoweLanCaster.exe` away from its `Resources` and `tools` folders.
3. Sideload `DoweLanCaster-Roku.zip` by following [Roku Setup](Roku-Setup).
4. Run `DoweLanCaster.exe`.
5. Allow the application through Windows Firewall when prompted, limited to private networks.
6. Select **Scan for Roku Devices**, choose the Roku, and test the **Remote** tab.

## Install from source

From the repository root:

```powershell
.\SETUP-DEPENDENCIES.cmd
dotnet restore
dotnet build
```

The dependency setup installs FFmpeg/ffprobe under `tools\ffmpeg` and yt-dlp under `tools\yt-dlp`.

To build complete distributable packages instead:

```powershell
.\BUILD-RELEASE.cmd
```

The output is placed in `dist`:

- `DoweLanCaster-Windows-x64.zip`
- `DoweLanCaster-Roku.zip`

## First-run checklist

- The Roku and PC are on the same local network.
- The active Windows network is marked **Private**.
- The Windows firewall allows Dowe LanCaster on private networks.
- The Dowe LanCaster Roku receiver has been sideloaded.
- FFmpeg is detected in the **Diagnostics** tab.
- yt-dlp is detected if you plan to use webpage URLs in Link Cast.

Next: [Roku Setup](Roku-Setup) and the [Casting Guide](Casting-Guide).

