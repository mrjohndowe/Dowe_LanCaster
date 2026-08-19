# Troubleshooting

Start with the application's **Diagnostics** tab. It reports the selected Roku, local IP, FFmpeg and yt-dlp availability, detected encoders, and recent status messages.

## No Roku devices found

1. Confirm the Roku and PC are on the same non-guest LAN.
2. Confirm the Windows network profile is Private.
3. Disable client/AP isolation in the router if enabled.
4. Check that TCP port 8060 is reachable and not blocked by security software.
5. Enter the Roku IP and select **Add Roku by IP**.
6. If the manual connection works, SSDP discovery is being filtered; casting can still work through the saved manual address.

## Remote works, but casting does not

This usually means the PC can reach Roku port 8060, but the Roku cannot reach the PC's stream server.

- Allow Dowe LanCaster through the firewall on private networks.
- Check the applicable TCP port: 8765, 8766, 8767, or 8768.
- Temporarily disconnect a VPN or enable its local-network access option.
- Verify Diagnostics shows the expected LAN IP, not a virtual adapter address.
- Ensure the sideloaded Roku receiver matches the current release.

## FFmpeg not found

Run `SETUP-DEPENDENCIES.cmd` from the repository root, or keep the `tools\ffmpeg` folder beside the packaged application. A release installation should contain `ffmpeg.exe` and `ffprobe.exe` under that folder.

## yt-dlp not found or link analysis fails

- Run `SETUP-DEPENDENCIES.cmd`, or restore `tools\yt-dlp\yt-dlp.exe` in the application folder.
- Test a direct media URL to separate extractor trouble from streaming trouble.
- Some sites change frequently; update yt-dlp and retry.
- DRM, paywalls, protected playback, authentication gates, and site access controls are intentionally not bypassed.

## Black screen or playback failure

- For Link, Live, or Folder Cast, try **CPU (libx264)** before a hardware encoder.
- Lower the quality/bitrate for a congested wireless network.
- For File Cast, test a Roku-compatible H.264/AAC MP4 or use Folder Cast to transcode the file.
- Re-sideload `DoweLanCaster-Roku.zip` if the receiver is from an older version.
- Try a short known-good video to distinguish content-specific errors.

## No live audio

- Choose **System Audio (Default Output)** to capture normal Windows playback.
- Make sure audio is actively playing through the current Windows default output.
- If using a microphone or other input, confirm FFmpeg lists that DirectShow device.
- Retry with **No Audio** to determine whether video capture itself is healthy.

## Build fails because files are in use

Close Dowe LanCaster before rebuilding. From PowerShell, a stuck development instance can be stopped with:

```powershell
Get-Process DoweLanCaster -ErrorAction SilentlyContinue | Stop-Process -Force
```

Then run `dotnet restore`, `dotnet clean`, and `dotnet build`.

