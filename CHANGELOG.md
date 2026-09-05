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

## [0.9.5.32] - 2026-09-04

### Added

- Added a GitHub Actions workflow that updates the supported-version table in
  `SECURITY.md` whenever a release is published or manually selected.

### Changed

- Connected supported-version maintenance directly to the release workflow.
- Serialized release metadata updates so changelog and security-policy commits
  cannot race each other.

## [0.9.5.31] - 2026-09-04

### Changed

- Replaced comparison-link placeholders with the actual changes for every documented release.
- Updated GitHub releases to show commit-based change details instead of a comparison-link placeholder.
- Updated the changelog workflow to create future entries from the release's real commit descriptions.

## [0.9.5.30] - 2026-09-04

### Fixed

- Kept one Roku receiver session active between Folder Cast videos.
- Kept the final video frame visible while the next item is prepared.
- Skipped the Roku launch animation when Folder Cast supplies a control URL.

## [0.9.5.29] - 2026-09-03

### Fixed

- Repaired Roku Private Listening audio playback and connection reporting.

### Changed

- Centralized encoder, frame-rate, bitrate, and audio defaults in Settings.

## [0.9.5.28] - 2026-09-03

### Added

- Added Settings and formatted in-app Changelog tabs.

## [0.9.5.27] - 2026-09-03

### Added

- Added real Roku Private Listening support using the authorized RPListening helper.

## [0.9.5.26] - 2026-09-03

### Fixed

- Preserved the Remote tab layout while the remote is open in its pop-out window.

## [0.9.5.25] - 2026-09-03

### Fixed

- Kept the Roku receiver open while advancing through Folder Cast videos.

## [0.9.5.24] - 2026-09-03

### Changed

- Updated embedded and pop-out controls with Roku-style button shapes.

## [0.9.5.23] - 2026-09-03

### Changed

- Matched the pop-out remote controls and layout to the embedded remote.

## [0.9.5.21] - 2026-09-02

### Changed

- Made the Roku app launcher default to a six-column grid without horizontal scrolling.

## [0.9.5.20] - 2026-09-02

### Fixed

- Restored the embedded remote automatically when the pop-out closes.

## [0.9.5.19] - 2026-09-02

### Fixed

- Reopened Dowe LanCaster after an installed update completes.

## [0.9.5.18] - 2026-09-02

### Changed

- Moved the Roku remote controls into the resizable pop-out window and added a
  compact Pop-out Remote button at the top-right of the Remote section.
- Matched the pop-out remote's button colors, rounded layout, directional pad,
  playback controls, and volume controls to the in-app remote.

### Fixed

- Removed the inactive in-app remote panel so the app no longer presents its
  cast-only PC audio monitor as Roku Private Listening.


## [0.9.5.17] - 2026-09-02

### Fixed

- Fixed the pop-out Remote window Windows build by explicitly using the WPF
  button type where Windows Forms is also available to the application.


## [0.9.5.16] - 2026-09-02

### Added

- Added a separate, resizable pop-out Roku Remote window with navigation,
  playback, power, volume buttons, and typed volume control.


## [0.9.5.15] - 2026-09-02

### Added

- Added Roku Remote volume entry with a 0–100 value and a Set Volume button.
- Added Headphone Mode to play the active Dowe LanCaster stream through the
  Windows default output, such as connected headphones.

### Changed

- Stopping a Link, Live, Folder, or File cast now stops local headphone
  playback as well.


## [0.9.5.14] - 2026-08-30

### Added

- Recognized signed TeraBox `/share/streaming` URLs as HLS sources for Link
  Cast, including the browser-style request headers needed to fetch the stream.
- Attached the Roku receiver ZIP as its own GitHub Release asset in future
  releases, as well as including it in the portable Windows ZIP.


## [0.9.5.13] - 2026-08-30

### Changed

- Restored manual encoder selection. Link Cast, Live Cast, and Folder Cast now
  use the encoder selected in their dropdown instead of overriding it when a
  stream starts.
- Added public TeraBox video share links to Link Cast and a clear message when
  a share does not expose a directly playable public stream.


## [0.9.5.12] - 2026-08-30

### Changed

- Published the synchronized Windows, installer, and Roku version 0.9.5.12 packages.

## [0.9.5.10] - 2026-08-28

### Changed

- Published the synchronized Windows, installer, and Roku version 0.9.5.10 packages.

## [0.9.5.9] - 2026-08-28


## [0.9.5.8] - 2026-08-26


## [0.9.5.7] - 2026-08-26


## [0.9.5.6] - 2026-08-26


## [0.9.5.5] - 2026-08-22


## [0.9.5.4] - 2026-08-20


## [0.9.0] - 2026-08-20


## [0.9.1] - 2026-08-20


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

[Unreleased]: https://github.com/mrjohndowe/Dowe_LanCaster/compare/v0.9.5.30...HEAD
[0.9.5.30]: https://github.com/mrjohndowe/Dowe_LanCaster/releases/tag/v0.9.5.30
[0.9.5.29]: https://github.com/mrjohndowe/Dowe_LanCaster/releases/tag/v0.9.5.29
[0.9.5.28]: https://github.com/mrjohndowe/Dowe_LanCaster/releases/tag/v0.9.5.28
[0.9.5.27]: https://github.com/mrjohndowe/Dowe_LanCaster/releases/tag/v0.9.5.27
[0.9.5.26]: https://github.com/mrjohndowe/Dowe_LanCaster/releases/tag/v0.9.5.26
[0.9.5.25]: https://github.com/mrjohndowe/Dowe_LanCaster/releases/tag/v0.9.5.25
[0.9.5.24]: https://github.com/mrjohndowe/Dowe_LanCaster/releases/tag/v0.9.5.24
[0.9.5.23]: https://github.com/mrjohndowe/Dowe_LanCaster/releases/tag/v0.9.5.23
[0.9.5.21]: https://github.com/mrjohndowe/Dowe_LanCaster/releases/tag/v0.9.5.21
[0.9.5.20]: https://github.com/mrjohndowe/Dowe_LanCaster/releases/tag/v0.9.5.20
[0.9.5.19]: https://github.com/mrjohndowe/Dowe_LanCaster/releases/tag/v0.9.5.19
[0.9.5.18]: https://github.com/mrjohndowe/Dowe_LanCaster/compare/v0.9.5.17...v0.9.5.18
[0.9.5.17]: https://github.com/mrjohndowe/Dowe_LanCaster/releases/tag/v0.9.5.17
[0.9.5.16]: https://github.com/mrjohndowe/Dowe_LanCaster/releases/tag/v0.9.5.16
[0.9.5.15]: https://github.com/mrjohndowe/Dowe_LanCaster/releases/tag/v0.9.5.15
[0.9.5.14]: https://github.com/mrjohndowe/Dowe_LanCaster/releases/tag/v0.9.5.14
[0.9.5.13]: https://github.com/mrjohndowe/Dowe_LanCaster/releases/tag/v0.9.5.13
[0.9.5.12]: https://github.com/mrjohndowe/Dowe_LanCaster/releases/tag/v0.9.5.12
[0.9.5.10]: https://github.com/mrjohndowe/Dowe_LanCaster/releases/tag/v0.9.5.10
[0.9.5.9]: https://github.com/mrjohndowe/Dowe_LanCaster/releases/tag/v0.9.5.9
[0.9.5.8]: https://github.com/mrjohndowe/Dowe_LanCaster/releases/tag/v0.9.5.8
[0.9.5.7]: https://github.com/mrjohndowe/Dowe_LanCaster/releases/tag/v0.9.5.7
[0.9.5.6]: https://github.com/mrjohndowe/Dowe_LanCaster/releases/tag/v0.9.5.6
[0.9.5.5]: https://github.com/mrjohndowe/Dowe_LanCaster/releases/tag/v0.9.5.5
[0.9.5.4]: https://github.com/mrjohndowe/Dowe_LanCaster/releases/tag/v0.9.5.4
[0.9.0]: https://github.com/mrjohndowe/Dowe_LanCaster/releases/tag/v0.9.0
[0.9.1]: https://github.com/mrjohndowe/Dowe_LanCaster/releases/tag/v0.9.1
[0.8.0]: https://github.com/mrjohndowe/Dowe_LanCaster/releases/tag/v0.8.0
[0.7.1]: https://github.com/mrjohndowe/Dowe_LanCaster/releases/tag/v0.7.1
[0.7.0]: https://github.com/mrjohndowe/Dowe_LanCaster/releases/tag/v0.7.0
[0.6.0]: https://github.com/mrjohndowe/Dowe_LanCaster/releases/tag/v0.6.0
[0.4.0]: https://github.com/mrjohndowe/Dowe_LanCaster/blob/main/docs/RELEASE-v0.4.0.md
