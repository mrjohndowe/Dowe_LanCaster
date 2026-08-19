# Dowe LanCaster Phase 2

Phase 2 adds direct PC-to-Roku file streaming.

## Included
- Media file picker
- Local ASP.NET Core HTTP server on port 8765
- HTTP byte-range support
- Automatic LAN address selection
- Roku receiver launch
- Stream URL handoff
- SceneGraph Video playback
- Stop casting
- Existing Roku remote and app launcher

## First test
Use an MP4 containing H.264/AVC video and AAC audio.

## Roku receiver
Run `scripts\Package-Roku.ps1`, then sideload `dist\DoweLanCaster-Roku.zip`.

## Firewall
Allow Dowe LanCaster on Windows Private networks so the Roku can access port 8765.

## Next
Phase 3 adds FFmpeg inspection, direct-play detection, automatic transcoding, HLS, and hardware encoders.
