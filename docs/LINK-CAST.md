# Link Cast

Link Cast accepts an HTTP/HTTPS page URL and attempts to locate a public,
non-DRM media stream using yt-dlp.

Pipeline:

    Page URL
       |
       v
    yt-dlp
       |
       v
    Direct media/HLS URL + required HTTP headers
       |
       v
    FFmpeg
       |
       v
    H.264 + AAC / HLS
       |
       v
    Dowe LanCaster local server :8767
       |
       v
    Roku receiver

## Supported behavior

- Direct media URLs
- Many public video pages supported by yt-dlp
- Live sources when the extractor returns a live media URL
- On-demand sources
- HTTP headers supplied by the extractor
- FFmpeg reconnection on network interruptions
- NVIDIA NVENC / AMD AMF / Intel QSV / CPU H.264 encoding

## Not supported

Dowe LanCaster does not bypass DRM, subscription/paywall controls, or
site access restrictions. A recognized page can still fail if the site's
media requires protected playback or if the extractor cannot obtain a
single playable audio/video URL.

## Setup

Run:

    SETUP-DEPENDENCIES.cmd

This installs:
- FFmpeg
- ffprobe
- yt-dlp

## Use

1. Sideload `dist\DoweLanCaster-Roku.zip`.
2. Launch the Windows app.
3. Select your Roku.
4. Open Link Cast.
5. Paste a video webpage URL.
6. Click Analyze Link.
7. Review the title/protocol/source type.
8. Pick an encoder and quality.
9. Click Stream to Roku.

Link Cast serves its Roku HLS output on port 8767.
