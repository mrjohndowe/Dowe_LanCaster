import { useState, useEffect } from "react";

type IconName =
  | "arrow-left"
  | "arrow-right"
  | "back"
  | "chevron-down"
  | "chevron-left"
  | "chevron-right"
  | "chevron-up"
  | "home"
  | "keyboard"
  | "more"
  | "mute"
  | "pause"
  | "play"
  | "power"
  | "refresh"
  | "rewind"
  | "speaker-down"
  | "speaker-up"
  | "spark"
  | "volume";

interface RokuDevice {
  endpoint: string;
  name: string;
}

interface ApiResponse {
  ok: boolean;
  message?: string;
  connected?: boolean;
  roku?: string;
  devices?: RokuDevice[];
}

function Icon({ name, size = 18 }: { name: IconName; size?: number }) {
  const common = {
    width: size,
    height: size,
    viewBox: "0 0 24 24",
    fill: "none",
    stroke: "currentColor",
    strokeWidth: 1.8,
    strokeLinecap: "round" as const,
    strokeLinejoin: "round" as const,
    "aria-hidden": true,
  };

  switch (name) {
    case "arrow-left":
      return <svg {...common}><path d="M19 12H5M11 18l-6-6 6-6" /></svg>;
    case "arrow-right":
      return <svg {...common}><path d="M5 12h14m-6-6 6 6-6 6" /></svg>;
    case "back":
      return <svg {...common}><path d="M10 6 4 12l6 6" /><path d="M4 12h13a4 4 0 0 1 4 4v2" /></svg>;
    case "chevron-down":
      return <svg {...common}><path d="m6 9 6 6 6-6" /></svg>;
    case "chevron-left":
      return <svg {...common}><path d="m15 6-6 6 6 6" /></svg>;
    case "chevron-right":
      return <svg {...common}><path d="m9 6 6 6-6 6" /></svg>;
    case "chevron-up":
      return <svg {...common}><path d="m6 15 6-6 6 6" /></svg>;
    case "home":
      return <svg {...common}><path d="m3 11 9-7 9 7" /><path d="M5 10v9h14v-9M9 19v-5h6v5" /></svg>;
    case "keyboard":
      return <svg {...common}><rect x="3" y="6" width="18" height="12" rx="2" /><path d="M7 10h.01M10 10h.01M13 10h.01M16 10h.01M7 14h7M17 14h.01" /></svg>;
    case "more":
      return <svg {...common}><circle cx="5" cy="12" r="1" fill="currentColor" stroke="none" /><circle cx="12" cy="12" r="1" fill="currentColor" stroke="none" /><circle cx="19" cy="12" r="1" fill="currentColor" stroke="none" /></svg>;
    case "mute":
      return <svg {...common}><path d="M5 10v4h4l5 4V6l-5 4H5Z" /><path d="m18 9 4 6m0-6-4 6" /></svg>;
    case "play":
      return <svg {...common} fill="currentColor" stroke="none"><path d="m8 5 11 7-11 7V5Z" /></svg>;
    case "pause":
      return <svg {...common} fill="currentColor" stroke="none"><path d="M7 5h4v14H7zM13 5h4v14h-4z" /></svg>;
    case "power":
      return <svg {...common}><path d="M12 3v9" /><path d="M7.05 5.93a8 8 0 1 0 9.9 0" /></svg>;
    case "refresh":
      return <svg {...common}><path d="M20 11a8 8 0 0 0-14.9-3L3 11" /><path d="M3 5v6h6M4 13a8 8 0 0 0 14.9 3L21 13" /><path d="M21 19v-6h-6" /></svg>;
    case "rewind":
      return <svg {...common} fill="currentColor" stroke="none"><path d="m5 12 8-7v14l-8-7Zm7 0 8-7v14l-8-7Z" /></svg>;
    case "speaker-down":
      return <svg {...common}><path d="M4 10v4h4l5 4V6l-5 4H4Z" /><path d="M17 10v4" /></svg>;
    case "speaker-up":
      return <svg {...common}><path d="M4 10v4h4l5 4V6l-5 4H4Z" /><path d="M17 9a4 4 0 0 1 0 6M19 6a8 8 0 0 1 0 12" /></svg>;
    case "spark":
      return <svg {...common}><path d="m12 3 1.7 5.3L19 10l-5.3 1.7L12 17l-1.7-5.3L5 10l5.3-1.7L12 3ZM19 16l.7 2.3L22 19l-2.3.7L19 22l-.7-2.3L16 19l2.3-.7L19 16Z" /></svg>;
    case "volume":
      return <svg {...common}><path d="M4 10v4h4l5 4V6l-5 4H4Z" /><path d="M17 9a4 4 0 0 1 0 6" /></svg>;
    default:
      return null;
  }
}

function ActionButton({
  label,
  icon,
  tone = "quiet",
  onClick,
  className = "",
  ariaLabel,
}: {
  label: string;
  icon?: IconName;
  tone?: "quiet" | "accent" | "power";
  onClick: () => void;
  className?: string;
  ariaLabel?: string;
}) {
  return (
    <button
      className={`action-button action-${tone} ${className}`}
      type="button"
      onClick={onClick}
      aria-label={ariaLabel ?? label}
    >
      {icon ? <Icon name={icon} size={18} /> : null}
      <span>{label}</span>
    </button>
  );
}

function App() {
  const [volume, setVolume] = useState("");
  const [remoteText, setRemoteText] = useState("");
  const [lastAction, setLastAction] = useState("Remote ready");
  const [notice, setNotice] = useState("");
  const [connected, setConnected] = useState(false);
  const [rokuName, setRokuName] = useState("");
  const [rokuIp, setRokuIp] = useState("");
  const [discoveredDevices, setDiscoveredDevices] = useState<RokuDevice[]>([]);
  const [isDiscovering, setIsDiscovering] = useState(false);
  const [isConnecting, setIsConnecting] = useState(false);
  // Roku does not expose a universal playback-state endpoint. Start with the
  // least surprising action (Play); after a confirmed keypress the control
  // becomes Pause and continues to use Roku's single Play key for toggling.
  const [isPlaybackPaused, setIsPlaybackPaused] = useState(true);

  const announce = (message: string) => {
    setLastAction(message);
    setNotice(message);
    window.setTimeout(() => setNotice(""), 2200);
  };

  const apiCall = async (body: any): Promise<ApiResponse> => {
    const response = await fetch("api/remote.php", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    });
    const data = await response.json();
    if (!response.ok) {
      throw new Error(data.message || "Roku request failed");
    }
    return data;
  };

  const checkStatus = async () => {
    try {
      const data = await apiCall({ action: "status" });
      setConnected(data.connected || false);
      setRokuName(data.roku || "");
    } catch (error) {
      console.error("Status check failed:", error);
    }
  };

  const discoverRoku = async () => {
    setIsDiscovering(true);
    setDiscoveredDevices([]);
    announce("Searching for Roku devices...");
    try {
      const data = await apiCall({ action: "discover" });
      if (!data.devices || data.devices.length === 0) {
        announce("No Roku devices found. Enter IP manually.");
        setDiscoveredDevices([]);
      } else {
        setDiscoveredDevices(data.devices || []);
        announce(`Found ${data.devices.length} Roku device(s)`);
      }
    } catch (error) {
      announce(`Discovery failed: ${error instanceof Error ? error.message : "Unknown error"}`);
    } finally {
      setIsDiscovering(false);
    }
  };

  const connectToRoku = async (ip?: string) => {
    setIsConnecting(true);
    const targetIp = ip || rokuIp;
    announce("Connecting to Roku...");
    try {
      const data = await apiCall({ action: "connect", ip: targetIp || "discover" });
      setConnected(true);
      setRokuName(data.roku || "");
      announce(data.message || "Connected to Roku");
      await checkStatus();
    } catch (error) {
      announce(`Connection failed: ${error instanceof Error ? error.message : "Unknown error"}`);
    } finally {
      setIsConnecting(false);
    }
  };

  const disconnect = async () => {
    try {
      await apiCall({ action: "disconnect" });
      setConnected(false);
      setRokuName("");
      setIsPlaybackPaused(true);
      announce("Disconnected from Roku");
    } catch (error) {
      announce(`Disconnect failed: ${error instanceof Error ? error.message : "Unknown error"}`);
    }
  };

  const sendRemoteKey = async (key: string, label = key): Promise<boolean> => {
    if (!connected) {
      announce("Not connected to Roku");
      return false;
    }
    try {
      await apiCall({ action: "command", command: key });
      announce(`${label} command sent`);
      return true;
    } catch (error) {
      announce(`${label} failed: ${error instanceof Error ? error.message : "Unknown error"}`);
      return false;
    }
  };

  const togglePlayPause = async () => {
    const action = isPlaybackPaused ? "Play" : "Pause";
    if (await sendRemoteKey("play_pause", action)) {
      setIsPlaybackPaused((paused) => !paused);
    }
  };

  const sendValueCommand = async (command: string, value: string, label: string) => {
    if (!connected) {
      announce("Not connected to Roku");
      return;
    }
    try {
      await apiCall({ action: "command", command, value });
      announce(`${label} sent${value ? `: ${value}` : ""}`);
    } catch (error) {
      announce(`${label} failed: ${error instanceof Error ? error.message : "Unknown error"}`);
    }
  };

  const handleVolume = () => {
    sendValueCommand("volume", volume, "Volume");
  };

  const handleText = () => {
    sendValueCommand("text", remoteText, "Text");
  };

  const selectDevice = (device: RokuDevice) => {
    const ip = device.endpoint.replace("http://", "").replace(":8060", "");
    setRokuIp(ip);
    announce(`Selected: ${device.name}`);
  };

  useEffect(() => {
    checkStatus();
  }, []);

  return (
    <div className="app-shell">
      <div className="ambient ambient-one" />
      <div className="ambient ambient-two" />

      <header className="app-header">
        <div className="brand-lockup">
          <div className="brand-mark" aria-hidden="true">
            <span />
            <span />
            <span />
          </div>
          <div>
            <p className="eyebrow">Dowe LanCaster</p>
            <h1>Roku remote</h1>
          </div>
        </div>
        <div className="header-status" aria-label="Roku connection status">
          <span className={`status-dot ${connected ? "status-connected" : "status-disconnected"}`} />
          <span>{connected ? rokuName || "Connected" : "Not connected"}</span>
          {connected && (
            <>
              <span className="status-divider" />
              <button
                className="disconnect-btn"
                type="button"
                onClick={disconnect}
                aria-label="Disconnect from Roku"
              >
                Disconnect
              </button>
            </>
          )}
        </div>
      </header>

      <main className="remote-stage">
        {!connected && (
          <section className="pairing-section" aria-label="Roku device pairing">
            <div className="pairing-card">
              <h2>Connect to your Roku</h2>
              <p>Find a Roku automatically, or enter its local IP address.</p>

              <button
                className="discover-btn"
                type="button"
                onClick={discoverRoku}
                disabled={isDiscovering}
              >
                {isDiscovering ? "Searching..." : "Discover Roku Devices"}
              </button>

              <div className="ip-input-group">
                <label htmlFor="roku-ip">Roku IP Address</label>
                <input
                  id="roku-ip"
                  type="text"
                  inputMode="decimal"
                  placeholder="Example: 10.0.0.10"
                  value={rokuIp}
                  onChange={(e) => setRokuIp(e.target.value)}
                />
              </div>

              <button
                className="connect-btn"
                type="button"
                onClick={() => connectToRoku()}
                disabled={isConnecting || !rokuIp}
              >
                {isConnecting ? "Connecting..." : "Connect to Roku"}
              </button>

              {discoveredDevices.length > 0 && (
                <div className="device-list">
                  <p className="device-list-label">Found devices:</p>
                  {discoveredDevices.map((device, index) => (
                    <button
                      key={index}
                      className="device-item"
                      type="button"
                      onClick={() => selectDevice(device)}
                    >
                      {device.name}
                      <span className="device-ip">
                        {device.endpoint.replace("http://", "").replace(":8060", "")}
                      </span>
                    </button>
                  ))}
                </div>
              )}
            </div>
          </section>
        )}

        {connected && (
          <section className="remote-device" aria-label="Dowe LanCaster Roku remote">
          <div className="device-topline">
            <div>
              <span className="device-kicker">Now controlling</span>
              <strong>Living room Roku</strong>
            </div>
          </div>

          <div className="last-action" aria-live="polite">
            <span className="last-action-dot" />
            <span>{lastAction}</span>
          </div>

          <div className="primary-actions" aria-label="Primary remote controls">
            <ActionButton label="Back" icon="back" onClick={() => sendRemoteKey("back")} />
            <ActionButton label="Home" icon="home" tone="accent" onClick={() => sendRemoteKey("home")} />
            <ActionButton label="Replay" icon="refresh" onClick={() => sendRemoteKey("replay")} />
            <ActionButton label="Power" icon="power" tone="power" onClick={() => sendRemoteKey("power")} />
          </div>

          <div className="remote-divider" />

          <section className="navigation-section" aria-label="Navigation pad">
            <p className="section-label">Navigation</p>
            <div className="dpad">
              <button className="dpad-button dpad-up" type="button" aria-label="Up" onClick={() => sendRemoteKey("up")}>
                <Icon name="chevron-up" size={24} />
              </button>
              <button className="dpad-button dpad-left" type="button" aria-label="Left" onClick={() => sendRemoteKey("left")}>
                <Icon name="chevron-left" size={24} />
              </button>
              <button className="dpad-button dpad-select" type="button" aria-label="Select" onClick={() => sendRemoteKey("select", "Select")}>
                <span>OK</span>
              </button>
              <button className="dpad-button dpad-right" type="button" aria-label="Right" onClick={() => sendRemoteKey("right")}>
                <Icon name="chevron-right" size={24} />
              </button>
              <button className="dpad-button dpad-down" type="button" aria-label="Down" onClick={() => sendRemoteKey("down")}>
                <Icon name="chevron-down" size={24} />
              </button>
            </div>
          </section>

          <section className="media-section" aria-label="Playback and volume controls">
            <div className="section-heading">
              <p className="section-label">Playback</p>
              <span className="section-caption">Media controls</span>
            </div>
            <div className="media-grid">
              <ActionButton label="Rev" icon="rewind" onClick={() => sendRemoteKey("rev")} />
              <ActionButton
                label={isPlaybackPaused ? "Play" : "Pause"}
                icon={isPlaybackPaused ? "play" : "pause"}
                tone="accent"
                ariaLabel={isPlaybackPaused ? "Play" : "Pause"}
                onClick={togglePlayPause}
              />
              <ActionButton label="Fwd" icon="arrow-right" onClick={() => sendRemoteKey("fwd")} />
            </div>
            <div className="media-grid volume-actions">
              <ActionButton label="Vol -" icon="speaker-down" onClick={() => sendRemoteKey("volume_down")} />
              <ActionButton label="Mute" icon="mute" onClick={() => sendRemoteKey("mute")} />
              <ActionButton label="Vol +" icon="speaker-up" onClick={() => sendRemoteKey("volume_up")} />
            </div>
          </section>

          <section className="form-section" aria-label="Set Roku volume">
            <label className="field-label" htmlFor="roku-volume">
              <Icon name="volume" size={15} />
              Set volume
              <span>0 - 100</span>
            </label>
            <div className="input-action-row">
              <input
                id="roku-volume"
                type="number"
                min="0"
                max="100"
                inputMode="numeric"
                placeholder="Roku volume"
                value={volume}
                onChange={(event) => setVolume(event.target.value)}
              />
              <button className="inline-submit" type="button" onClick={handleVolume}>
                Set
                <Icon name="arrow-right" size={15} />
              </button>
            </div>
          </section>

          <section className="text-section" aria-label="Send keyboard text">
            <div className="section-heading">
              <label className="section-label" htmlFor="roku-text">
                <Icon name="keyboard" size={15} />
                Keyboard text
              </label>
              <span className="section-caption">Type or dictate</span>
            </div>
            <div className="text-input-wrap">
              <input
                id="roku-text"
                type="text"
                placeholder="Type text to your Roku..."
                value={remoteText}
                onChange={(event) => setRemoteText(event.target.value)}
                onKeyDown={(event) => {
                  if (event.key === "Enter") handleText();
                }}
              />
              <button className="text-submit" type="button" aria-label="Send text to Roku" onClick={handleText}>
                <Icon name="arrow-right" size={17} />
              </button>
            </div>
          </section>

          <button className="companion-link" type="button" onClick={() => announce("Back to companion sections")}>
            <Icon name="arrow-left" size={16} />
            Back to companion sections
            <Icon name="more" size={15} />
          </button>
        </section>
        )}
      </main>

      <p className="footer-note">Dowe LanCaster <span>•</span> Roku control surface</p>

      <div className={`notice ${notice ? "notice-visible" : ""}`} role="status" aria-live="polite">
        <Icon name="spark" size={16} />
        <span>{notice}</span>
      </div>
    </div>
  );
}

export default App;
