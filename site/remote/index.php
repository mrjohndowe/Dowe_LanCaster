<!doctype html>
<html>

<head>
    <meta name="viewport" content="width=device-width,initial-scale=1">
    <link rel="stylesheet" href="assets/remote.css?v=<?= getVersionNumber() ?>">
    <script src="assets/remote.js?v=<?= getVersionNumber() ?>" defer></script>
    <title>Dowe LanCaster Remote</title>
</head>

<body>
    <header><b>◈ DOWE LANCASTER • ROKU REMOTE</b><button id="compare">▥ BEFORE VS AFTER</button><button id="disconnect" hidden>DISCONNECT</button></header>
    <nav id="stages"><span>01 BASELINE</span><span>02 CHASSIS</span><span>03 D-PAD</span><span>04 MEDIA</span><span>05 AUDIO</span><b>06 FINAL BUILD</b></nav>
    <main>
        <section id="pairing" class="pair">
            <h1>Connect to your Roku</h1>
            <p>Find a Roku automatically, or enter its local IP address.</p>
            <button id="discover" type="button">DISCOVER ROKU DEVICES</button>
            <label>ROKU IP ADDRESS <input id="roku-ip" inputmode="decimal" placeholder="Example: 10.0.0.42" aria-label="Roku IP address"></label>
            <button id="connect" type="button">CONNECT TO ROKU</button>
            <p id="pair-status">No Roku selected.</p>
            <div id="roku-devices" aria-live="polite"></div>
        </section>
        <section class="remote-area" id="controls" hidden>
            <p id="note">AFTER • TACTILE HARDWARE EDITION</p>
            <div class="remote canonical">
                <div class="brandbar"><b>DOWE LANCASTER</b><span>ROKU REMOTE · PAIRED</span></div>
                <div class="system-pills"><button data-command="back">↩ Back</button><button data-command="home" class="accent">⌂ Home</button><button data-command="replay">⟳ Replay</button><button data-command="power" class="power">⏻ Power</button></div>
                <div class="radial-pad"><button data-command="up">▲</button>
                    <div><button data-command="left">◀</button><button data-command="select" class="accent ok">OK</button><button data-command="right">▶</button></div><button data-command="down">▼</button>
                </div>
                <div class="transport-deck"><button data-command="rev">Rev</button><button data-command="play_pause" class="accent">Play</button><button data-command="fwd">Fwd</button></div>
                <div class="volume-deck"><button data-command="volume_down">Vol −</button><button data-command="mute">Mute</button><button data-command="volume_up">Vol +</button></div>
                <form class="dock" onsubmit="return false"><input id="volume" type="number" value="28" min="0" max="100" aria-label="Roku volume 0 to 100"><button id="set">Set Volume</button></form>
                <p class="lbl">Keyboard Text</p>
                <form class="dock" onsubmit="return false"><input id="text" maxlength="160" aria-label="Text to send to Roku" placeholder="Type or dictate text to your Roku…"><button id="send">Send Text</button></form>
            </div>
        </section>
        <aside>
            <h1>Operational Remote Dashboard</h1>
            <p>Build stages, command parity, telemetry and direct Roku ECP transport status.</p>
            <div class="debug"><b>TRANSPORT</b><span id="mode">ROKU NOT CONNECTED</span><small id="endpoint">Discover a Roku or enter its local IP address</small><small id="response">Last response: awaiting Roku connection</small><small>Commands are sent directly to the selected Roku on your LAN</small></div>
            <h2>LIVE BUILD OUTPUT</h2>
            <pre id="buildout">FINAL BUILD ACTIVE
API whitelist locked · payload validation ready.</pre>
            <div id="comparison" hidden>
                <h2>COMPARISON SUMMARY</h2>
                <p>Stage 1: flat stacked keys · Stage 6: tactile radial D-pad, inline controls, persistent status.</p>
            </div>
            <h2>EVENT TELEMETRY</h2>
            <ol id="events">
                <li>Awaiting remote action…</li>
            </ol>
        </aside>
    </main>
    <footer>DIRECT ROKU LAN CONTROL · NO PC COMPANION REQUIRED · THE SELECTED ROKU IP STAYS IN THIS SESSION</footer>
</body>

</html>

<?php
function getVersionNumber()
{
    $v1 = rand(1, 99);
    $v2 = rand(1, 99);
    $v3 = rand(1, 99);
    $versionNumber = $v1 . '.' . $v2 . '.' . $v3;

    return $versionNumber;
}
function console_log($data)
{

    $display = '<script>';
    $display .= 'console.log(' . json_encode($data) . ');';
    $display .= '</script>';
    echo $display;
}
$versionNumber = getVersionNumber();
// file_put_contents('storage/logs/oldVersion.log', 'Version Number: ' . date('m/d/Y H:i:s') . ' ' . $versionNumber . PHP_EOL, FILE_APPEND);
console_log('Version Number: v' . $versionNumber);
// console_log('Old Version Number: v' . $oldVersion);

?>
