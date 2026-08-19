# Dedicated Intro Video

Dowe LanCaster uses a dedicated startup video file:

`src\DoweLanCaster.Windows\Resources\intro.mp4`

The still-image fallback remains:

`src\DoweLanCaster.Windows\Resources\intro.png`

## Behavior

- `IntroWindow` plays `intro.mp4` at startup.
- The main application opens automatically when the video finishes.
- If the MP4 is missing or cannot be played, the PNG fallback is displayed briefly and the app still starts.
- The intro video is copied to the Windows build/publish output under `Resources\intro.mp4`.

## Replacing the intro

Replace:

`Resources\intro.mp4`

with another MP4 using the same filename.

Recommended encoding:

- H.264 video
- yuv420p pixel format
- 1280x720 or 1920x1080
- AAC audio if the intro includes sound
- short duration, ideally 3 to 8 seconds

No source-code change is required when replacing the file.
