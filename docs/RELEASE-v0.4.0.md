# Dowe LanCaster v0.4.0

This package consolidates the working Roku sideload method and the Phase 4 live-cast fixes.

## Fixes included

- WPF `Application` ambiguity fixed.
- WPF `OpenFileDialog` ambiguity fixed.
- WPF `Button` ambiguity fixed.
- `HttpClient` namespace fixes.
- `System.IO` namespace fixes.
- Roku package preserves `source/` and `components/`.
- Roku receives `mediaType` before `streamUrl`.
- Roku detects `.m3u8` as HLS even if the type parameter is absent.
- Removed the unnecessary Roku `content.live` assignment.
- Live HLS uses MPEG-TS.
- Live segments changed to 2 seconds.
- Playlist increased to 10 segments.
- Delete threshold increased to 5.
- Removed `append_list`.
- H.264 High Profile / Level 4.1 added.
- Keyframes aligned to live segment cadence.
- FFmpeg waits until a playlist contains media segments before launching Roku.
- Roku ZIP included prebuilt.
- One-command FFmpeg setup and Windows release publishing scripts added.
