# Network and Ports

Dowe LanCaster is designed for a trusted local network. The Windows PC hosts media endpoints, and the Roku connects back to those endpoints during playback.

| Port | Direction | Purpose |
| --- | --- | --- |
| UDP 1900 | PC ↔ LAN | SSDP Roku discovery. |
| TCP 8060 | PC → Roku | Roku ECP discovery probes, remote commands, app listing, and receiver launch. |
| TCP 8765 | Roku → PC | File Cast media server. |
| TCP 8766 | Roku → PC | Live Cast HLS server. |
| TCP 8767 | Roku → PC | Link Cast HLS server. |
| TCP 8768 | Roku → PC | Folder Cast HLS server. |

## Firewall guidance

Allow Dowe LanCaster through Windows Firewall on **Private** networks. Avoid opening these ports to the public internet or enabling access on an untrusted public network.

If remote commands work but video does not, port 8060 is reachable but the Roku may be unable to connect back to ports 8765–8768. Check Windows Firewall, third-party security software, VPN routing, and router client-isolation settings.

## Network requirements

- The Roku and PC should be on the same IPv4 LAN/subnet.
- Guest Wi-Fi commonly blocks one client from reaching another; use the normal trusted LAN.
- A VPN can cause the app to select an address the Roku cannot reach. Temporarily disconnect it or adjust its local-LAN access setting.
- Wi-Fi access-point isolation must be disabled for peer-to-peer LAN traffic.
- For stable device selection, consider a DHCP reservation for the Roku.

The streaming servers bind to all PC interfaces, while the app advertises a selected LAN IP to the Roku. The Diagnostics tab shows the current address and is useful when multiple adapters are present.

