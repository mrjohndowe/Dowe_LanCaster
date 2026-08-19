# Dowe LanCaster v0.7.0

Dowe LanCaster is a Windows-to-Roku LAN casting, playlist, live-streaming,
link-streaming, and remote-control application.

## New in v0.7.0

### Folder Cast
Stream a complete folder of videos as a Roku playlist.

- Folder picker
- Recursive subfolder scanning
- MP4/MKV/AVI/WebM/MOV/MPEG/TS/WMV/FLV support
- FFmpeg H.264/AAC transcoding for mixed-format folders
- Previous / Play / Next / Stop
- Auto-play next
- Shuffle
- Repeat Off / One / All
- Move items up/down
- Sort A-Z / Z-A
- Double-click to start from any item
- Failed-item skipping
- Current item / playlist position
- Saved folder preferences

### Dedicated intro video
Startup now plays:

`src\DoweLanCaster.Windows\Resources\intro.mp4`

The original PNG remains as a fallback:

`src\DoweLanCaster.Windows\Resources\intro.png`

Replace `intro.mp4` with another H.264 MP4 using the same filename to change the startup video without changing code.

## Existing features

- Link Cast
- Direct media URL bypass
- yt-dlp webpage extraction
- Live desktop / monitor / window cast
- Native Windows system-audio loopback
- Local File Cast
- Roku remote
- SSDP + LAN Roku discovery
- Add Roku by IP
- Saved preferences
- Diagnostics
- Centered/scaled Roku receiver

## Ports

- 8060 Roku ECP
- 8765 File Cast
- 8766 Live Cast
- 8767 Link Cast
- 8768 Folder Cast

## Build

Stop any running app before rebuilding:

```powershell
Get-Process DoweLanCaster -ErrorAction SilentlyContinue | Stop-Process -Force
dotnet restore
dotnet clean
dotnet build
```

## Roku

Ready-to-sideload receiver:

`dist\DoweLanCaster-Roku.zip`

## Git checkpoint

The release package includes:

`DoweLanCaster-v0.7.0.bundle`

This Git bundle contains the v0.7.0 checkpoint commit and tag.

See `docs\GIT-CHECKPOINT.txt`.
