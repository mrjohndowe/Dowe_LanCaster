# Phase 1 - Roku Control Foundation

## Goal

Establish reliable PC-to-Roku LAN communication before adding streaming.

## Complete

- SSDP Roku discovery
- ECP device info
- ECP remote commands
- Installed-app query
- App launching
- Text entry
- Roku receiver scaffold

## Test checklist

1. Open `DoweLanCaster.sln`.
2. Build the Windows project.
3. Make sure Windows and Roku are on the same LAN.
4. Run Dowe LanCaster.
5. Click `Scan for Roku Devices`.
6. Select your Roku.
7. Test directional buttons.
8. Test Home/Back.
9. Test Play/Pause.
10. Test Volume if supported by your Roku TV.
11. Refresh installed apps.
12. Double-click an installed app.

## Next

Phase 2 will add:

- ASP.NET Core/Kestrel local media server
- File picker
- Local MP4 streaming
- Roku receiver auto-launch
- Playback URL handoff
