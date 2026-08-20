# Changelog

All notable changes to Dowe LanCaster are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed

- Prepared the Windows application and installer for version 0.9.0.
- Added the license file to the installer configuration.
- Improved Link Cast extraction for public webpage URLs using the bundled
  `yt-dlp` with fallback detection for standard video, source, Open Graph,
  JSON, MP4, WebM, and HLS references.
- Forwarded required referrer and browser headers to FFmpeg when streaming
  extracted media.
- Completed Dark Mode styling for dropdowns, tabs, buttons, disabled controls,
  lists, selections, hover states, and standard WPF controls.

### Security

- Link Cast does not bypass DRM, authentication, paywalls, or access controls.

## [0.8.0] - 2026-08-19

### Added

- Redesigned Roku Remote interface with a rounded remote body, directional pad,
  dedicated OK button, navigation controls, playback controls, volume controls,
  keyboard input, and an installed-app launcher.
- Voice control through Windows speech recognition for common Roku navigation,
  playback, and volume commands.
- Optional application-wide light and dark themes with saved preferences.
- Automated synchronization of release documentation to the project wiki.

### Changed

- Consolidated the Windows release as Dowe LanCaster v0.8.0.

## [0.7.1] - 2026-08-19

### Added

- Portable ZIP release artifact for the Windows application.
- Windows installer release artifact.

## [0.7.0] - 2026-08-19

### Added

- Folder Cast for streaming an entire folder of videos as a Roku playlist.
- Recursive subfolder scanning and support for MP4, MKV, AVI, WebM, MOV, MPEG,
  TS, WMV, and FLV files.
- FFmpeg H.264/AAC transcoding for mixed-format folders.
- Playlist controls for previous, play, next, stop, shuffle, repeat, sorting,
  reordering, and starting from any selected item.
- Automatic next-item playback, failed-item skipping, playlist position, and
  saved folder preferences.
- Dedicated startup video with an image fallback.

### Fixed

- Added missing namespace qualifications and imports used by Folder Cast and
  media-link extraction.
- Updated ClickOnce publishing version settings.

## [0.6.0] - 2026-08-19

### Added

- Link Cast with direct media URL detection and `yt-dlp` webpage extraction.
- Live desktop, monitor, and window casting.
- Native Windows system-audio loopback.
- Local File Cast.
- Roku remote control.
- SSDP and LAN Roku discovery, with manual Roku IP entry.
- Saved preferences and diagnostics.
- Centered and scaled Roku receiver playback.
- Automated GitHub Actions builds and release publishing.

## [0.4.0]

### Added

- Prebuilt, sideload-ready Roku receiver package.
- One-command FFmpeg setup and Windows release publishing scripts.

### Fixed

- Resolved WPF type and namespace ambiguities affecting application, file
  picker, button, HTTP, and file-system code.
- Preserved the Roku package's `source/` and `components/` directories.
- Sent `mediaType` to Roku before `streamUrl`.
- Detected `.m3u8` streams as HLS when the type parameter is absent.
- Removed the unnecessary Roku `content.live` assignment.
- Improved live HLS reliability with MPEG-TS, two-second segments, a ten-segment
  playlist, a five-segment deletion threshold, aligned H.264 keyframes, and
  High Profile Level 4.1 encoding.
- Waited for playable media segments before launching the Roku receiver.

[Unreleased]: https://github.com/mrjohndowe/Dowe_LanCaster/compare/v0.8.0...HEAD
[0.8.0]: https://github.com/mrjohndowe/Dowe_LanCaster/releases/tag/v0.8.0
[0.7.1]: https://github.com/mrjohndowe/Dowe_LanCaster/releases/tag/v0.7.1
[0.7.0]: https://github.com/mrjohndowe/Dowe_LanCaster/releases/tag/v0.7.0
[0.6.0]: https://github.com/mrjohndowe/Dowe_LanCaster/releases/tag/v0.6.0
[0.4.0]: https://github.com/mrjohndowe/Dowe_LanCaster/blob/main/docs/RELEASE-v0.4.0.md
