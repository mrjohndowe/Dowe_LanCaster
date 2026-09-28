<?php
// Serve the React remote from the preview build
$reactAppPath = __DIR__ . '/index.html';

if (file_exists($reactAppPath)) {
    // Read the React app HTML
    $html = file_get_contents($reactAppPath);

    // Inject connection UI and API integration script
    $connectionUI = <<< 'HTML'
<div id="roku-connection" style="position:fixed;top:0;left:0;right:0;background:#090A0F;border-bottom:1px solid #7C5CFF;padding:16px;z-index:1000;display:block;">
    <div style="max-width:1200px;margin:0 auto;display:flex;align-items:center;gap:12px;flex-wrap:wrap;">
        <span style="color:#F3F4F6;font-weight:600;">Roku Connection</span>
        <input id="roku-ip" type="text" placeholder="Roku IP (e.g., 10.0.0.105)" style="background:#16161d;border:1px solid #7C5CFF33;color:#F3F4F6;padding:8px 12px;border-radius:8px;flex:1;min-width:200px;">
        <button id="discover-btn" style="background:#7C5CFF;color:#090A0F;border:none;padding:8px 16px;border-radius:8px;font-weight:600;cursor:pointer;">Discover</button>
        <button id="connect-btn" style="background:#6EEBAD;color:#090A0F;border:none;padding:8px 16px;border-radius:8px;font-weight:600;cursor:pointer;">Connect</button>
        <button id="disconnect-btn" style="background:#FF8795;color:#090A0F;border:none;padding:8px 16px;border-radius:8px;font-weight:600;cursor:pointer;display:none;">Disconnect</button>
        <span id="connection-status" style="color:#9CA3AF;font-size:12px;">Not connected</span>
        <div id="discovered-devices" style="display:none;position:absolute;top:100%;left:0;background:#16161d;border:1px solid #7C5CFF33;border-radius:8px;padding:8px;margin-top:8px;min-width:200px;max-height:200px;overflow-y:auto;">
            <button id="hide-devices" style="display:block;width:100%;background:#7C5CFF;color:#090A0F;border:none;padding:4px 8px;border-radius:4px;font-size:11px;font-weight:600;cursor:pointer;margin-bottom:8px;">Hide</button>
        </div>
    </div>
</div>
HTML;

    $apiScript = <<< 'SCRIPT'
<script>
// Connect React UI to PHP backend API
(function() {
    const apiEndpoint = 'api/remote.php';
    let isConnected = false;

    async function apiCall(body) {
        const response = await fetch(apiEndpoint, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body)
        });
        const data = await response.json();
        if (!response.ok) {
            throw new Error(data.message || 'Roku request failed');
        }
        return data;
    }

    async function checkConnection() {
        try {
            const data = await apiCall({ action: 'status' });
            isConnected = data.connected;
            updateConnectionUI(data.connected, data.roku);
        } catch (error) {
            console.error('Status check failed:', error.message);
        }
    }

    function updateConnectionUI(connected, rokuName) {
        const connectBtn = document.getElementById('connect-btn');
        const disconnectBtn = document.getElementById('disconnect-btn');
        const status = document.getElementById('connection-status');

        if (connected) {
            connectBtn.style.display = 'none';
            disconnectBtn.style.display = 'block';
            status.textContent = 'Connected to ' + (rokuName || 'Roku');
            status.style.color = '#6EEBAD';
        } else {
            connectBtn.style.display = 'block';
            disconnectBtn.style.display = 'none';
            status.textContent = 'Not connected';
            status.style.color = '#9CA3AF';
        }
    }

    // Hide discovered devices
    document.getElementById('hide-devices').addEventListener('click', () => {
        document.getElementById('discovered-devices').style.display = 'none';
    });

    // Discover Roku devices
    document.getElementById('discover-btn').addEventListener('click', async () => {
        const status = document.getElementById('connection-status');
        const devicesDiv = document.getElementById('discovered-devices');

        status.textContent = 'Discovering...';
        devicesDiv.style.display = 'none';

        try {
            const data = await apiCall({ action: 'discover' });

            if (data.devices && data.devices.length > 0) {
                devicesDiv.innerHTML = '';
                data.devices.forEach(device => {
                    const button = document.createElement('button');
                    button.textContent = device.name + ' (' + device.endpoint.replace('http://', '').replace(':8060', '') + ')';
                    button.style.cssText = 'display:block;width:100%;text-align:left;padding:8px;background:#090A0F;border:1px solid #7C5CFF33;color:#F3F4F6;border-radius:4px;cursor:pointer;margin-bottom:4px;';
                    button.addEventListener('click', () => {
                        document.getElementById('roku-ip').value = device.endpoint.replace('http://', '').replace(':8060', '');
                    });
                    devicesDiv.appendChild(button);
                });
                devicesDiv.style.display = 'block';
                status.textContent = 'Found ' + data.devices.length + ' device(s)';
            } else {
                status.textContent = 'No devices found';
            }
        } catch (error) {
            status.textContent = 'Discovery failed: ' + error.message;
        }
    });

    // Connect to Roku
    document.getElementById('connect-btn').addEventListener('click', async () => {
        const ip = document.getElementById('roku-ip').value.trim();
        const status = document.getElementById('connection-status');
        const devicesDiv = document.getElementById('discovered-devices');

        if (!ip) {
            status.textContent = 'Enter Roku IP address';
            return;
        }

        status.textContent = 'Connecting...';

        try {
            const data = await apiCall({ action: 'connect', ip: ip });
            isConnected = true;
            updateConnectionUI(true, data.roku);
            // Hide discovered devices after successful connection
            devicesDiv.style.display = 'none';
        } catch (error) {
            status.textContent = 'Connection failed: ' + error.message;
        }
    });

    // Disconnect from Roku
    document.getElementById('disconnect-btn').addEventListener('click', async () => {
        try {
            await apiCall({ action: 'disconnect' });
            isConnected = false;
            updateConnectionUI(false);
        } catch (error) {
            console.error('Disconnect failed:', error.message);
        }
    });

    // Enhanced command mapping with more variations
    const commandMap = {
        // Navigation
        'home': 'home',
        'back': 'back',
        'up': 'up',
        'down': 'down',
        'left': 'left',
        'right': 'right',
        'select': 'select',
        'ok': 'select',
        '▲': 'up',
        '▼': 'down',
        '◀': 'left',
        '▶': 'right',
        '↑': 'up',
        '↓': 'down',
        '←': 'left',
        '→': 'right',

        // System
        'power': 'power',
        'replay': 'replay',
        'instantreplay': 'replay',

        // Playback
        'play': 'play_pause',
        'pause': 'play_pause',
        'play/pause': 'play_pause',
        'rev': 'rev',
        'rewind': 'rev',
        'fwd': 'fwd',
        'fastforward': 'fwd',

        // Volume
        'volume up': 'volume_up',
        'vol +': 'volume_up',
        'volume_up': 'volume_up',
        'volume down': 'volume_down',
        'vol -': 'volume_down',
        'volume_down': 'volume_down',
        'mute': 'mute',
        'volume mute': 'mute'
    };

    // Hook into button clicks with enhanced matching
    document.addEventListener('click', async (e) => {
        const button = e.target.closest('button');
        if (!button) return;

        // Skip connection buttons
        if (button.id === 'discover-btn' || button.id === 'connect-btn' || button.id === 'disconnect-btn' || button.id === 'hide-devices') return;

        const buttonText = button.textContent?.toLowerCase().trim();
        const buttonClass = button.className?.toLowerCase();
        const buttonAriaLabel = button.getAttribute('aria-label')?.toLowerCase();
        const buttonInnerHTML = button.innerHTML?.toLowerCase();

        // Try multiple matching strategies
        let command = null;

        // Direct text match
        if (commandMap[buttonText]) {
            command = commandMap[buttonText];
        }

        // Partial text match
        if (!command) {
            for (const [key, value] of Object.entries(commandMap)) {
                if (buttonText?.includes(key) || buttonText?.includes(key.replace('_', ' '))) {
                    command = value;
                    break;
                }
            }
        }

        // HTML content match (for arrow symbols)
        if (!command) {
            for (const [key, value] of Object.entries(commandMap)) {
                if (buttonInnerHTML?.includes(key)) {
                    command = value;
                    break;
                }
            }
        }

        // Class name match
        if (!command) {
            for (const [key, value] of Object.entries(commandMap)) {
                if (buttonClass?.includes(key)) {
                    command = value;
                    break;
                }
            }
        }

        // ARIA label match
        if (!command && buttonAriaLabel) {
            for (const [key, value] of Object.entries(commandMap)) {
                if (buttonAriaLabel?.includes(key)) {
                    command = value;
                    break;
                }
            }
        }

        if (command) {
            e.preventDefault();
            if (!isConnected) {
                console.log('Not connected to Roku - command:', command);
                alert('Please connect to a Roku first');
                return;
            }
            try {
                await apiCall({ action: 'command', command });
                console.log('Command sent:', command);
            } catch (error) {
                console.error('Command failed:', error.message);
                alert('Command failed: ' + error.message);
            }
        } else {
            console.log('No command matched for button:', buttonText, buttonClass, buttonAriaLabel, buttonInnerHTML);
        }
    });

    // Handle text input
    document.addEventListener('submit', async (e) => {
        const form = e.target;
        const input = form.querySelector('input[type="text"]');
        if (input && input.value.trim() && input.id !== 'roku-ip') {
            e.preventDefault();
            if (!isConnected) {
                console.log('Not connected to Roku');
                return;
            }
            try {
                await apiCall({ action: 'command', command: 'text', value: input.value.trim() });
                console.log('Text sent:', input.value.trim());
                input.value = '';
            } catch (error) {
                console.error('Text send failed:', error.message);
            }
        }
    });

    // Check connection status on load
    checkConnection();

    console.log('Roku Remote API integration loaded');
})();
</script>
SCRIPT;

    // Insert connection UI before closing body tag and script after it
    $html = str_replace('</body>', $connectionUI . $apiScript . '</body>', $html);

    echo $html;
    exit;
}

// Fallback to original PHP remote if React build doesn't exist
