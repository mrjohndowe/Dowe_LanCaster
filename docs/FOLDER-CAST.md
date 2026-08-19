# Folder Cast

Folder Cast streams an entire folder of videos to a Roku as a managed playlist.

## Supported local formats

- MP4 / M4V
- MOV
- MKV
- WebM
- AVI
- MPEG / MPG
- TS / M2TS / MTS
- WMV
- FLV

FFmpeg converts each item to Roku-friendly H.264/AAC HLS before playback.

## Features

- Choose a folder
- Include subfolders
- Auto-play next
- Previous / Next / Stop
- Double-click any item to start there
- Move selected item up/down
- Sort A-Z / Z-A
- Shuffle the list immediately
- Random-next shuffle mode
- Repeat Off / One / All
- Skip failed items
- Current item and playlist-position display
- Saved last-used folder
- Saved subfolder / autoplay / repeat settings

## Folder Cast port

Folder Cast uses local HTTP/HLS port:

8768

## First test

1. Select your Roku.
2. Open Folder Cast.
3. Choose a folder with a few short videos.
4. Leave Auto-play next enabled.
5. Use CPU (libx264) and 4000 kbps for the first compatibility test.
6. Click Play.

After that works, switch to NVENC, AMF, or Quick Sync.
