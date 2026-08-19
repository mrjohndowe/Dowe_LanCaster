# Dowe LanCaster Phase 4

## Added

- Entire desktop live capture
- Individual monitor live capture
- Application-window live capture
- Mouse pointer capture
- Optional DirectShow audio capture
- FFmpeg discovery
- Hardware H.264 encoder detection
  - NVIDIA NVENC
  - AMD AMF
  - Intel Quick Sync
  - CPU libx264 fallback
- 30/60 FPS modes
- 4/8/12/18 Mbps profiles
- Rolling low-latency HLS
- Local live-stream server on port 8766
- Updated Roku receiver for HLS/live playback

## FFmpeg setup

Run:

    .\scripts\Check-FFmpeg.ps1

If not found, put ffmpeg.exe and ffprobe.exe into:

    tools\ffmpeg\

or install FFmpeg and add it to PATH.

## Roku receiver

The receiver changed for live HLS support, so sideload it again:

    .\scripts\Package-Roku.ps1

Then upload:

    dist\DoweLanCaster-Roku.zip

The packaging script intentionally uses tar.exe because that packaging method
preserved the Roku source/components directories correctly on the test TV.

## First live test

1. Scan/select the Roku.
2. Open Live Cast.
3. Select Entire Desktop.
4. Leave audio OFF.
5. Select the first detected encoder.
6. Use 30 FPS.
7. Use 8000 kbps.
8. Start Live Cast.

Expected URL:

    http://PC-LAN-IP:8766/live/index.m3u8

## Audio note

The current audio mode uses FFmpeg DirectShow capture devices. If Windows
exposes Stereo Mix or a virtual loopback device, select it. Not every audio
driver exposes PC-output loopback through DirectShow.

## Ports

- 8060 Roku ECP
- 8765 file casting
- 8766 live HLS

Allow Dowe LanCaster through Windows Firewall on Private networks.
