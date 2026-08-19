# Casting Guide

Before casting, select a Roku at the top of the application and confirm the receiver is sideloaded. For transcoded workflows, begin with the CPU encoder and a moderate quality setting; switch to a detected hardware encoder after the first successful test.

## Link Cast

Use Link Cast for direct media URLs or public, non-DRM webpages supported by yt-dlp.

1. Paste an HTTP or HTTPS URL.
2. Select **Analyze Link**.
3. Review the detected title, protocol, and source type.
4. Choose an encoder and quality.
5. Select **Stream to Roku**.

Direct media URLs can bypass extraction. Webpage URLs are analyzed by yt-dlp, then FFmpeg reconnects to the media source and produces Roku-compatible H.264/AAC HLS on port 8767. Protected, paywalled, or extraction-blocked media is not supported.

## Live Cast

Use Live Cast for a desktop, monitor, or application window.

1. Choose a capture source.
2. Choose an audio source:
   - **System Audio (Default Output)** captures normal Windows playback through WASAPI loopback.
   - **No Audio** produces a silent stream.
   - A microphone or other DirectShow input can be selected when FFmpeg detects it.
3. Choose an encoder and quality.
4. Start the live stream.

Live Cast produces HLS on port 8766. System Audio does not require a Stereo Mix driver; the application sends loopback PCM to FFmpeg through a Windows named pipe.

## Folder Cast

Folder Cast scans a folder into a managed video playlist.

1. Choose a folder.
2. Enable subfolder scanning if desired.
3. Arrange the playlist with sort, move, or shuffle controls.
4. Choose an encoder and quality. **CPU (libx264)** at roughly **4000 kbps** is a reliable first compatibility test.
5. Select an item and play it, or double-click the item where playback should begin.

Supported extensions include MP4, M4V, MOV, MKV, WebM, AVI, MPEG/MPG, TS/M2TS/MTS, WMV, and FLV. Each item is transcoded to Roku-friendly H.264/AAC HLS on port 8768.

Playlist controls include previous, play, next, stop, move up/down, A-Z/Z-A sorting, shuffle, repeat off/one/all, auto-play next, and failed-item skipping. The last folder and several playlist preferences are saved.

## File Cast

Use File Cast to serve one local file to the Roku.

1. Choose a local media file.
2. Select **Cast to Roku**.

The file server listens on port 8765. Because this workflow serves the selected file directly, playback still depends on the Roku supporting the file's container and codecs. Use Folder Cast when normalization through FFmpeg is preferable.

## Remote

The Remote tab sends ECP commands to the selected Roku. It provides navigation and playback keys, literal text entry, a list of installed apps, and app launching. Remote operations use TCP port 8060 and do not require a media stream.

