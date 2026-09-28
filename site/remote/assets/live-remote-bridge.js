(() => {
  "use strict";

  const endpoint = "api/remote.php";
  const commands = {
    Back: "back", Home: "home", Replay: "replay", Power: "power",
    Up: "up", Down: "down", Left: "left", Right: "right", Select: "select",
    Rev: "rev", Play: "play_pause", Pause: "play_pause", Fwd: "fwd",
    VolumeDown: "volume_down", VolumeUp: "volume_up", Mute: "mute"
  };
  let connected = false;
  let paused = true;

  const style = document.createElement("style");
  style.textContent = `
    #dowe-roku-connect { position:sticky; top:0; z-index:100; display:flex; align-items:center; justify-content:center; gap:8px; padding:10px 12px; background:#10131d; border-bottom:1px solid rgba(167,139,250,.42); color:#f3f4f6; font:600 12px system-ui; }
    #dowe-roku-connect label { color:#a78bfa; letter-spacing:.08em; text-transform:uppercase; font-size:10px; }
    #dowe-roku-ip { width:150px; border:1px solid rgba(255,255,255,.18); border-radius:8px; background:#181b26; color:#fff; padding:8px 10px; }
    #dowe-roku-connect button { border:0; border-radius:8px; background:#7c5cff; color:#fff; padding:8px 12px; font-weight:700; cursor:pointer; }
    #dowe-roku-status { color:#9ca3af; min-width:112px; }
    #dowe-roku-status.is-connected { color:#6eebad; }
    .dowe-remote-pending { transform:translateY(1px) scale(.98) !important; filter:brightness(.82); pointer-events:none; }
    @media (max-width:520px) { #dowe-roku-connect { flex-wrap:wrap; } #dowe-roku-status { width:100%; text-align:center; } }
  `;
  document.head.append(style);

  const bar = document.createElement("form");
  bar.id = "dowe-roku-connect";
  bar.innerHTML = `
    <label for="dowe-roku-ip">Roku connection</label>
    <input id="dowe-roku-ip" inputmode="decimal" autocomplete="off" placeholder="Roku IP address">
    <button type="submit">Connect</button>
    <span id="dowe-roku-status" role="status" aria-live="polite">Not connected</span>`;
  document.body.prepend(bar);

  const input = bar.querySelector("#dowe-roku-ip");
  const submit = bar.querySelector("button");
  const status = bar.querySelector("#dowe-roku-status");

  async function api(body) {
    const response = await fetch(endpoint, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body)
    });
    const data = await response.json();
    if (!response.ok || !data.ok) throw new Error(data.message || "Roku request failed");
    return data;
  }

  function setStatus(message, isConnected = false) {
    status.textContent = message;
    status.classList.toggle("is-connected", isConnected);
  }

  function setPlayButton(button) {
    const label = button.querySelector("span:last-child");
    if (label) label.textContent = paused ? "Play" : "Pause";
    button.title = `sendRemoteKey("${paused ? "Play" : "Pause"}")`;
    button.setAttribute("aria-label", paused ? "Play" : "Pause");
  }

  async function refreshConnection() {
    try {
      const data = await api({ action: "status" });
      connected = Boolean(data.connected);
      setStatus(connected ? `Connected to ${data.roku || "Roku"}` : "Not connected", connected);
    } catch {
      connected = false;
      setStatus("Connection check failed");
    }
  }

  bar.addEventListener("submit", async (event) => {
    event.preventDefault();
    const ip = input.value.trim();
    if (!ip) {
      setStatus("Enter the Roku IP address");
      input.focus();
      return;
    }
    submit.disabled = true;
    setStatus("Connecting...");
    try {
      const data = await api({ action: "connect", ip });
      connected = true;
      setStatus(`Connected to ${data.roku || "Roku"}`, true);
    } catch (error) {
      connected = false;
      setStatus(error.message || "Could not connect");
    } finally {
      submit.disabled = false;
    }
  });

  document.addEventListener("click", async (event) => {
    const button = event.target.closest("button");
    if (!button || button.closest("#dowe-roku-connect")) return;

    const titleMatch = button.title && button.title.match(/sendRemoteKey\("([^"]+)"\)/);
    const text = button.textContent.replace(/\s+/g, " ").trim();
    let key = titleMatch && titleMatch[1];
    if (!key && /Back$/.test(text) && text !== "Back to Companion Sections") key = "Back";
    if (!key && /Home$/.test(text)) key = "Home";
    if (!key && /Replay$/.test(text)) key = "Replay";
    if (!key && /Power$/.test(text)) key = "Power";

    let command = key && commands[key];
    let value = null;
    if (text === "Set Volume") {
      command = "volume";
      const volume = Array.from(document.querySelectorAll("input")).find((field) => field !== input && field.type === "number");
      value = volume && volume.value.trim();
    } else if (text === "Send Text to Roku") {
      command = "text";
      const textInput = Array.from(document.querySelectorAll("input")).find((field) => field !== input && field.type !== "number");
      value = textInput && textInput.value.trim();
    } else if (/Roku Private Listening/.test(text)) {
      command = "private_listening";
    } else if (/Voice Control/.test(text)) {
      command = "voice_control";
    }
    if (!command) return;

    // The bundled showcase remote only displays demo callbacks. Stop that
    // callback so each physical press produces one real Roku command.
    event.preventDefault();
    event.stopImmediatePropagation();

    if (!connected && command !== "private_listening" && command !== "voice_control") {
      setStatus("Connect to your Roku first");
      return;
    }

    button.classList.add("dowe-remote-pending");
    try {
      if ((command === "volume" || command === "text") && !value) {
        throw new Error(command === "volume" ? "Enter a volume from 0 to 100" : "Enter text to send to Roku");
      }
      await api({ action: "command", command, value });
      if (command === "play_pause") {
        paused = !paused;
        setPlayButton(button);
      }
      if (command === "text") {
        const textInput = Array.from(document.querySelectorAll("input")).find((field) => field !== input && field.type !== "number");
        if (textInput) textInput.value = "";
      }
      setStatus(command === "private_listening" ? "Private Listening toggled in Dowe LanCaster" : command === "voice_control" ? "Voice Control toggled in Dowe LanCaster" : `Sent ${key || command} to Roku`, connected);
    } catch (error) {
      setStatus(error.message || `Could not send ${key}`);
    } finally {
      button.classList.remove("dowe-remote-pending");
    }
  }, true);

  refreshConnection();
})();
