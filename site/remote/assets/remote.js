const controls = document.querySelector("#controls"),
  pair = document.querySelector("#pairing"),
  status = document.querySelector("#pair-status"),
  disc = document.querySelector("#disconnect"),
  events = document.querySelector("#events"),
  mode = document.querySelector("#mode"),
  volume = document.querySelector("#volume");
async function api(body) {
  let r = await fetch("api/remote.php", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    }),
    d = await r.json();
  if (!r.ok) throw Error(d.message);
  return d;
}
function log(t) {
  let x = document.createElement("li");
  x.textContent = new Date().toLocaleTimeString() + " — " + t;
  if (events.firstChild?.textContent.includes("Awaiting"))
    events.innerHTML = "";
  events.prepend(x);
}
async function refresh() {
  try {
    let d = await api({ action: "status" });
    controls.hidden = !d.paired;
    pair.hidden = d.paired;
    disc.hidden = !d.paired;
    mode.textContent = d.paired ? "PAIRED COMPANION" : "PAIRING REQUIRED";
  } catch {}
}
document.querySelector("#connect").onclick = async () => {
  status.textContent = "Discovering Dowe LanCaster on your local network…";
  try {
    let d = await api({
      action: "pair",
      code: document.querySelector("#pair-code").value,
    });
    status.textContent = d.message;
    await refresh();
  } catch (e) {
    status.textContent = e.message;
  }
};
disc.onclick = async () => {
  await api({ action: "disconnect" });
  await refresh();
};
async function command(c, value) {
  try {
    let d = await api({ action: "command", command: c, value });
    log(c + " · " + d.message);
  } catch (e) {
    log(c + " · " + e.message);
  }
}
document.querySelectorAll("[data-command]").forEach(
  (b) =>
    (b.onclick = () => {
      let c = b.dataset.command;
      if (c === "options") {
        log("Options · unavailable in companion protocol");
        return;
      }
      if (c === "replay") c = "replay";
      if (c === "play_pause") c = "play_pause";
      command(c);
    }),
);
document.querySelector("#set").onclick = () => command("volume", +volume.value);
document.querySelector("#send").onclick = () => {
  let t = document.querySelector("#text");
  if (t.value.trim()) command("text", t.value.trim());
};
refresh();
