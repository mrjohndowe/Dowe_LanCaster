# Dowe LanCaster v0.9.0

Dowe LanCaster is a Windows-to-Roku LAN casting, playlist, live-streaming,
link-streaming, and remote-control application.

## New in v0.9.0

### Improved Link Cast webpage extraction
Link Cast now resolves public webpage URLs with bundled yt-dlp and falls back
to detecting standard video, source, Open Graph, JSON, MP4, WebM, and HLS
references embedded in the page. Required Referer and browser headers are
passed to FFmpeg. DRM, authentication, paywalls, and access controls are not
bypassed.

### Complete Dark Mode control styling
Dark Mode now provides readable colors for dropdowns, tabs, buttons, disabled
controls, lists, selections, hover states, and standard WPF system controls.

## New in v0.8.0

### Redesigned Roku Remote
The Remote tab now looks and behaves more like a physical Roku remote.

- rounded remote body
- directional pad
- dedicated OK button
- Back / Home / Replay
- Rewind / Play-Pause / Fast Forward
- volume controls
- larger keyboard text input
- keyboard placeholder text
- installed Roku app launcher

### Voice Control
A microphone button enables Windows speech recognition for Roku commands.

Examples:

`Home`
`Back`
`Up`
`Down`
`Left`
`Right`
`OK`
`Play`
`Pause`
`Fast Forward`
`Rewind`
`Volume Up`
`Volume Down`
`Mute`

Voice recognition uses the default Windows microphone.

### Dark Mode
The entire Windows application now supports optional light and dark themes.

The selected theme is saved between launches.

## Existing features

- Folder Cast
- Link Cast
- TeraBox Open Platform account connection
- encrypted TeraBox credentials and OAuth tokens
- TeraBox account folder/file browser and Roku video casting
- direct media URL detection
- yt-dlp webpage extraction
- Live Cast
- Windows system-audio loopback
- File Cast
- Roku SSDP + LAN discovery
- Add Roku by IP
- Diagnostics
- saved preferences
- dedicated startup `intro.mp4`

## Ports

- 8060 Roku ECP
- 8765 File Cast
- 8766 Live Cast
- 8767 Link Cast
- 8767 TeraBox Cast
- 8768 Folder Cast

## Build

Close Dowe LanCaster before rebuilding:

```powershell
Get-Process DoweLanCaster -ErrorAction SilentlyContinue | Stop-Process -Force
dotnet restore
dotnet clean
dotnet build
```

The Windows build restores both NAudio and System.Speech from NuGet.

## Git checkpoint

The release package includes:

`DoweLanCaster-v0.8.0.bundle`

See `docs\GIT-CHECKPOINT.txt`.
