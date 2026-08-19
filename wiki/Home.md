# Dowe LanCaster

Dowe LanCaster is a Windows-to-Roku application for casting media and controlling a Roku over a local network. It can stream local files, folders and playlists, public non-DRM web media, or a live Windows display/window, and it includes a Roku remote and app launcher.

The current application version is **0.7.0**. The Windows client targets **Windows x64 / .NET 8**, and the companion Roku channel is installed through Roku developer mode.

## Start here

1. Follow [Installation](Installation) to prepare the Windows application and Roku receiver.
2. Complete [Roku Setup](Roku-Setup) and select the Roku in Dowe LanCaster.
3. Choose a workflow in the [Casting Guide](Casting-Guide).
4. If discovery or playback fails, use [Troubleshooting](Troubleshooting) and the app's **Diagnostics** tab.

## What it can do

| Feature | Purpose |
| --- | --- |
| Link Cast | Extract and transcode a public, non-DRM media URL or webpage with yt-dlp and FFmpeg. |
| Live Cast | Stream a Windows desktop, monitor, or application window, with optional system or input audio. |
| Folder Cast | Play a folder of videos as a managed Roku playlist with shuffle, repeat, sorting, and automatic advance. |
| File Cast | Serve a local media file directly to the Roku. |
| Remote | Send Roku remote commands, enter text, and launch installed Roku apps. |
| Diagnostics | Show detected dependencies, selected device, network address, encoder information, and recent status. |

## Important limits

Dowe LanCaster does not bypass DRM, subscriptions, paywalls, protected playback, or site access controls. The Roku and Windows PC must be reachable on the same local network, and Windows Firewall must allow the application on private networks.

## Wiki contents

- [Installation](Installation)
- [Roku Setup](Roku-Setup)
- [Casting Guide](Casting-Guide)
- [Network and Ports](Network-and-Ports)
- [Troubleshooting](Troubleshooting)
- [Architecture](Architecture)
- [Development](Development)
- [Building a Release](Building-a-Release)

