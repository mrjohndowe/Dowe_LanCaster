# System Audio

Dowe LanCaster v0.6.0 adds native Windows system-output capture.

The Live Cast audio selector includes:

- System Audio (Default Output)
- No Audio
- DirectShow microphone/input devices reported by FFmpeg
- Stereo Mix / What U Hear when the driver exposes them

System Audio (Default Output) uses NAudio WasapiLoopbackCapture and streams
raw PCM to FFmpeg through a Windows named pipe. FFmpeg encodes the audio as
AAC for the Roku HLS stream.

This means a Stereo Mix driver is no longer required for normal desktop audio.
