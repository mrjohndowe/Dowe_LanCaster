# Dowe LanCaster v0.6.0

Dowe LanCaster is a Windows-to-Roku LAN casting and remote-control application.

## v0.6.0 checkpoint

### Link Cast
- Direct `.mp4`, `.m3u8`, `.webm`, `.mov`, `.mkv`, and similar URLs bypass yt-dlp.
- Normal webpage URLs use yt-dlp when installed.
- FFmpeg converts extracted/direct media to Roku-friendly H.264/AAC HLS.
- Link Cast uses port 8767.

### Live Cast
- Entire desktop
- Individual monitors
- Application windows
- **System Audio (Default Output)** using Windows WASAPI loopback through NAudio
- DirectShow microphone/input fallbacks
- NVIDIA NVENC / AMD AMF / Intel Quick Sync / libx264
- 30/60 FPS and selectable bitrate
- Live Cast uses port 8766.

### Roku
- SSDP discovery plus local-LAN fallback scan
- Manual **Add Roku by IP**
- Remote control
- Installed app launcher
- Roku video player scales to the active design resolution and stays centered

### Quality of life
- Saved Roku IP
- Saved encoder
- Saved FPS
- Saved bitrate
- Saved audio source
- Diagnostics tab

## Dependencies

Run:

`SETUP-DEPENDENCIES.cmd`

This installs FFmpeg and yt-dlp. NAudio is restored automatically by .NET/NuGet.

## Build

Close any running Dowe LanCaster instance first:

```powershell
Get-Process DoweLanCaster -ErrorAction SilentlyContinue | Stop-Process -Force
dotnet clean
dotnet build
```

## Roku receiver

A ready-to-sideload ZIP is included:

`dist\DoweLanCaster-Roku.zip`

If you edit the Roku source:

```powershell
.\scripts\Package-Roku.ps1
```

## Git checkpoint

The generated package is committed and tagged as `v0.6.0`.

See `docs\GIT-CHECKPOINT.txt`.
