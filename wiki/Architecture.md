# Architecture

Dowe LanCaster contains a Windows WPF application and a Roku SceneGraph receiver.

```text
Media source
   │
   ├─ local file ───────────────────────────────┐
   ├─ folder item ── FFmpeg transcode ─────────┤
   ├─ display/window + audio ── FFmpeg ─────────┤
   └─ public URL ── yt-dlp ── FFmpeg ──────────┤
                                                  ▼
                                      Local HTTP/HLS server
                                      ports 8765–8768
                                                  │
                         Roku ECP launch on 8060  │
                                      ┌───────────┘
                                      ▼
                               Roku receiver channel
```

## Windows client

The Windows project is `src\DoweLanCaster.Windows` and targets `net8.0-windows`. It uses WPF for the interface, Windows Forms APIs where needed by capture/folder dialogs, ASP.NET Core for embedded media servers, and NAudio for WASAPI loopback system-audio capture.

Important service responsibilities:

- `RokuDiscoveryService`: SSDP discovery, LAN fallback scanning, and device-info probes.
- `RokuClient`: ECP keypresses, text input, app enumeration/launch, receiver launch, and media-player state.
- `MediaStreamingServer`: direct File Cast HTTP serving on port 8765.
- `LiveStreamingServer` and `LiveCaptureService`: live capture, FFmpeg, and HLS serving on port 8766.
- `UrlStreamCaptureService` and `MediaLinkExtractorService`: direct-media detection, yt-dlp extraction, FFmpeg conversion, and HLS on port 8767.
- `FolderPlaylistService` and `LocalFileHlsTranscoder`: playlist management and per-item HLS conversion on port 8768.
- `EncoderDetectionService`: detects supported FFmpeg CPU and hardware H.264 encoders.
- `SettingsService`: persists the last Roku and user preferences.

## Roku receiver

The Roku project is `src\DoweLanCaster.Roku`. Its `manifest`, `source`, `components`, and images are packaged into `DoweLanCaster-Roku.zip`. The channel accepts launch parameters from the Windows client, identifies HLS where necessary, and plays the advertised stream through a centered/scaled SceneGraph video component.

## Media formats

Transcoded workflows normalize video to H.264 and audio to AAC, delivered as HLS. File Cast intentionally serves the original file and therefore relies on native Roku codec/container compatibility.

## Security boundary

The application exposes unauthenticated local media endpoints while casting. It is intended for a trusted private LAN, not for port forwarding or public hosting. URL extraction respects DRM and site access controls.

