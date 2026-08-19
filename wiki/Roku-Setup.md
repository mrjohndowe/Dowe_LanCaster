# Roku Setup

Dowe LanCaster uses a companion Roku receiver. Install the packaged `DoweLanCaster-Roku.zip` through Roku's developer-mode sideload page.

## Sideload the receiver

1. Enable developer mode on the Roku using Roku's official developer-mode procedure.
2. Note the Roku IP address shown during developer setup.
3. From a PC on the same LAN, open the Roku developer web page at `http://ROKU-IP`.
4. Sign in with the developer credentials configured on the Roku.
5. Upload and install `dist\DoweLanCaster-Roku.zip` from a source checkout, or the copy included in the Windows release package.
6. Reinstall the package whenever the Roku receiver source changes or a release specifically requests it.

Roku permits one developer-sideloaded channel at a time. Installing another development channel can replace Dowe LanCaster.

## Connect from Windows

1. Launch Dowe LanCaster.
2. Select **Scan for Roku Devices**.
3. Choose the Roku from the device list.
4. If discovery finds nothing, enter the Roku's IP address and select **Add Roku by IP**.
5. Open **Remote** and send a harmless command, such as a directional key, to confirm connectivity.

The app discovers Roku devices through SSDP and a LAN fallback scan. Manual connection probes the Roku External Control Protocol (ECP) service on TCP port 8060.

## When the Roku IP changes

Dowe LanCaster saves the last Roku IP. If DHCP assigns a new address, scan again or enter the new address manually. A DHCP reservation in the router can keep the Roku address stable.

## Receiver behavior

The Windows app launches the sideloaded receiver and passes it the media type and a local HTTP/HLS stream URL. The receiver displays the stream centered and scaled on the Roku. Playback availability therefore depends on the Roku being able to reach the PC's LAN address and the relevant casting port.

See [Network and Ports](Network-and-Ports) for firewall details.

