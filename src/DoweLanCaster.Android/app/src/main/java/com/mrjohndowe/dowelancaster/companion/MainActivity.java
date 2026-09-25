package com.mrjohndowe.dowelancaster.companion;

import android.app.Activity;
import android.graphics.Color;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.content.SharedPreferences;
import android.text.InputType;
import android.widget.Toast;
import android.view.Gravity;
import android.view.ViewGroup;
import android.widget.Button;
import android.widget.EditText;
import android.widget.ImageView;
import android.widget.LinearLayout;
import android.widget.TextView;
import android.content.res.ColorStateList;
import android.graphics.drawable.GradientDrawable;

import org.json.JSONObject;

import java.io.OutputStream;
import java.net.HttpURLConnection;
import java.net.DatagramPacket;
import java.net.DatagramSocket;
import java.net.InetAddress;
import java.net.URI;
import java.nio.charset.StandardCharsets;
import java.util.UUID;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

public final class MainActivity extends Activity {
    private static final int MATCH = ViewGroup.LayoutParams.MATCH_PARENT;
    private static final int WRAP = ViewGroup.LayoutParams.WRAP_CONTENT;
    private final ExecutorService networkExecutor = Executors.newSingleThreadExecutor();
    private final Handler mainHandler = new Handler(Looper.getMainLooper());
    private volatile String discoveredEndpoint;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setTheme(R.style.Theme_DoweLanCaster);

        showIntroSplash();
    }

    private void showIntroSplash() {
        LinearLayout splash = new LinearLayout(this);
        splash.setOrientation(LinearLayout.VERTICAL);
        splash.setGravity(Gravity.CENTER);
        splash.setPadding(dp(32), dp(32), dp(32), dp(32));
        splash.setBackgroundColor(color(R.color.background));

        ImageView logo = new ImageView(this);
        logo.setImageResource(R.drawable.dowelancaster_icon);
        logo.setContentDescription("Dowe LanCaster logo");
        logo.setScaleType(ImageView.ScaleType.CENTER_INSIDE);
        splash.addView(logo, margins(MATCH, dp(190), 0, 0, 0, 20));

        TextView title = text("Dowe LanCaster", 30, R.color.text_primary);
        title.setGravity(Gravity.CENTER_HORIZONTAL);
        title.setTypeface(title.getTypeface(), android.graphics.Typeface.BOLD);
        splash.addView(title, margins(MATCH, WRAP, 0, 0, 0, 8));

        TextView subtitle = text("Android Companion", 18, R.color.accent);
        subtitle.setGravity(Gravity.CENTER_HORIZONTAL);
        splash.addView(subtitle, margins(MATCH, WRAP, 0, 0, 0, 12));

        TextView loading = text("Starting companion...", 14, R.color.text_secondary);
        loading.setGravity(Gravity.CENTER_HORIZONTAL);
        splash.addView(loading, margins(MATCH, WRAP, 0, 0, 0, 0));

        setContentView(splash);
        mainHandler.postDelayed(this::showPairingScreen, 1400);
    }

    private void showPairingScreen() {

        LinearLayout page = new LinearLayout(this);
        page.setOrientation(LinearLayout.VERTICAL);
        page.setPadding(dp(24), dp(32), dp(24), dp(24));
        page.setBackgroundColor(color(R.color.background));

        ImageView logo = new ImageView(this);
        logo.setImageResource(R.drawable.dowelancaster_icon);
        logo.setContentDescription("Dowe LanCaster logo");
        logo.setScaleType(ImageView.ScaleType.CENTER_INSIDE);
        page.addView(logo, margins(MATCH, dp(112), 0, 0, 0, 12));

        TextView title = text("Dowe LanCaster", 28, R.color.text_primary);
        title.setTypeface(title.getTypeface(), android.graphics.Typeface.BOLD);
        page.addView(title, margins(MATCH, WRAP, 0, 0, 0, 8));

        TextView subtitle = text("Android Companion", 18, R.color.accent);
        page.addView(subtitle, margins(MATCH, WRAP, 0, 0, 0, 28));

        TextView instructions = text(
                "Pair this phone with Dowe LanCaster on your PC. The PC will show the address and one-time pairing code.",
                16, R.color.text_secondary);
        page.addView(instructions, margins(MATCH, WRAP, 0, 0, 0, 20));

        TextView discovery = text("Looking for Dowe LanCaster on this Wi-Fi network...", 15, R.color.text_secondary);
        discovery.setGravity(Gravity.CENTER_HORIZONTAL);
        page.addView(discovery, margins(MATCH, WRAP, 0, 0, 0, 12));

        EditText endpoint = input("PC IP:PORT (example: 10.0.0.45:8770)");
        page.addView(endpoint, margins(MATCH, dp(48), 0, 0, 0, 12));

        EditText code = input("One-time pairing code");
        code.setInputType(InputType.TYPE_CLASS_NUMBER | InputType.TYPE_NUMBER_VARIATION_PASSWORD);
        page.addView(code, margins(MATCH, dp(54), 0, 0, 0, 16));

        Button pair = remoteButton("Pair with PC", "pair", R.color.accent);
        pair.setTextSize(16);
        page.addView(pair, margins(MATCH, dp(54), 0, 0, 0, 20));

        TextView status = text(
                "Enter the PC IP:PORT and one-time code shown in Dowe LanCaster Settings.",
                14, R.color.text_secondary);
        status.setGravity(Gravity.CENTER_HORIZONTAL);
        page.addView(status, margins(MATCH, WRAP, 0, 0, 0, 0));

        pair.setOnClickListener(view -> pairWithPc(page, endpoint.getText().toString(), code.getText().toString(), pair, status));

        setContentView(page);
        discoverPc(discovery, endpoint, status, pair);
    }

    private void discoverPc(TextView discovery, EditText endpointField, TextView status, Button pairButton) {
        networkExecutor.execute(() -> {
            String endpoint = null;
            try (DatagramSocket socket = new DatagramSocket()) {
                socket.setBroadcast(true);
                byte[] request = "DOWE_LANCASTER_DISCOVER".getBytes(StandardCharsets.UTF_8);
                DatagramPacket packet = new DatagramPacket(
                        request, request.length,
                        InetAddress.getByName("255.255.255.255"), 8771);
                socket.send(packet);
                socket.setSoTimeout(2500);
                byte[] response = new byte[128];
                DatagramPacket reply = new DatagramPacket(response, response.length);
                socket.receive(reply);
                String message = new String(reply.getData(), reply.getOffset(), reply.getLength(), StandardCharsets.UTF_8);
                if (message.startsWith("DOWE_LANCASTER_PC|")) {
                    endpoint = reply.getAddress().getHostAddress() + ":" + message.substring("DOWE_LANCASTER_PC|".length());
                }
            } catch (Exception ignored) {
                // The status text below explains that the PC was not discovered.
            }

            discoveredEndpoint = endpoint;
            final String foundEndpoint = endpoint;
            String discovered = foundEndpoint == null
                    ? "PC not found. Keep Dowe LanCaster open on the same Wi-Fi network."
                    : "PC found: " + foundEndpoint;
            mainHandler.post(() -> {
                discovery.setText(discovered);
                if (foundEndpoint != null) endpointField.setText(foundEndpoint);
                pairButton.setEnabled(true);
                status.setText(foundEndpoint == null
                        ? "Enter the PC IP:PORT manually, then enter the six-digit pairing code."
                        : "PC found. Enter the six-digit pairing code shown on the PC.");
            });
        });
    }

    private void pairWithPc(LinearLayout page, String endpointText, String codeText, Button pairButton, TextView status) {
        String endpoint = endpointText.trim();
        if (endpoint.isEmpty()) endpoint = discoveredEndpoint;
        String pairingCode = codeText.trim();
        if (endpoint == null || pairingCode.length() != 6) {
            status.setText("Enter the six-digit pairing code shown on the PC.");
            return;
        }
        final String targetEndpoint = endpoint;

        pairButton.setEnabled(false);
        status.setText("Connecting to the PC companion service...");
        networkExecutor.execute(() -> {
            String result;
            boolean paired = false;
            try {
                SharedPreferences preferences = getSharedPreferences("companion", MODE_PRIVATE);
                String deviceId = preferences.getString("device_id", null);
                if (deviceId == null) {
                    deviceId = UUID.randomUUID().toString();
                    preferences.edit().putString("device_id", deviceId).apply();
                }

                URI uri = URI.create("http://" + targetEndpoint + "/api/v1/pairing/approve");
                HttpURLConnection connection = (HttpURLConnection) uri.toURL().openConnection();
                connection.setRequestMethod("POST");
                connection.setConnectTimeout(5000);
                connection.setReadTimeout(5000);
                connection.setDoOutput(true);
                connection.setRequestProperty("Content-Type", "application/json");
                JSONObject payload = new JSONObject();
                payload.put("code", pairingCode);
                payload.put("deviceId", deviceId);
                byte[] body = payload.toString().getBytes(StandardCharsets.UTF_8);
                try (OutputStream output = connection.getOutputStream()) {
                    output.write(body);
                }

                int responseCode = connection.getResponseCode();
                paired = responseCode >= 200 && responseCode < 300;
                result = responseCode >= 200 && responseCode < 300
                        ? "Paired with Dowe LanCaster on the PC."
                        : "The PC rejected the pairing code (HTTP " + responseCode + ").";
                connection.disconnect();
            } catch (Exception exception) {
                result = "Could not reach the PC. Check the IP:PORT and that the companion service is running.";
            }

            String finalResult = result;
            boolean accepted = paired;
            mainHandler.post(() -> {
                if (accepted) {
                    showConnectedScreen(page);
                    return;
                }
                status.setText(finalResult);
                pairButton.setEnabled(true);
                Toast.makeText(this, finalResult, Toast.LENGTH_SHORT).show();
            });
        });
    }

    private void showConnectedScreen(LinearLayout page) {
        page.removeAllViews();

        ImageView logo = new ImageView(this);
        logo.setImageResource(R.drawable.dowelancaster_icon);
        logo.setContentDescription("Dowe LanCaster logo");
        logo.setScaleType(ImageView.ScaleType.CENTER_INSIDE);
        page.addView(logo, margins(MATCH, dp(96), 0, 0, 0, 12));

        TextView title = text("Connected to Dowe LanCaster", 24, R.color.text_primary);
        title.setGravity(Gravity.CENTER_HORIZONTAL);
        title.setTypeface(title.getTypeface(), android.graphics.Typeface.BOLD);
        page.addView(title, margins(MATCH, WRAP, 0, 0, 0, 8));

        TextView subtitle = text("Choose a section to control on the PC.", 16, R.color.text_secondary);
        subtitle.setGravity(Gravity.CENTER_HORIZONTAL);
        page.addView(subtitle, margins(MATCH, WRAP, 0, 0, 0, 24));

        String[] sections = {"Remote", "Link Cast", "Live Cast", "Folder Cast", "TeraBox", "Settings", "Diagnostics"};
        for (String section : sections) {
            Button sectionButton = new Button(this);
            sectionButton.setText(section);
            sectionButton.setAllCaps(false);
            sectionButton.setTextSize(16);
            sectionButton.setOnClickListener(view -> {
                sendTabCommand(section);
                if (section.equals("Remote")) {
                    showRemoteScreen(page);
                } else {
                    showSectionScreen(page, section);
                }
            });
            page.addView(sectionButton, margins(MATCH, dp(52), 0, 0, 0, 8));
        }

        TextView connected = text("Secure pairing accepted for this phone.", 14, R.color.text_secondary);
        connected.setGravity(Gravity.CENTER_HORIZONTAL);
        page.addView(connected, margins(MATCH, WRAP, 0, 16, 0, 0));
    }

    private void sendTabCommand(String tab) {
        if (discoveredEndpoint == null) return;
        networkExecutor.execute(() -> {
            try {
                HttpURLConnection connection = (HttpURLConnection)
                        URI.create("http://" + discoveredEndpoint + "/api/v1/commands/select-tab").toURL().openConnection();
                connection.setRequestMethod("POST");
                connection.setConnectTimeout(5000);
                connection.setReadTimeout(5000);
                connection.setDoOutput(true);
                connection.setRequestProperty("Content-Type", "application/json");
                JSONObject payload = new JSONObject();
                payload.put("tab", tab);
                try (OutputStream output = connection.getOutputStream()) {
                    output.write(payload.toString().getBytes(StandardCharsets.UTF_8));
                }
                connection.getResponseCode();
                connection.disconnect();
            } catch (Exception ignored) { }
        });
    }

    private void showRemoteScreen(LinearLayout page) {
        page.removeAllViews();
        page.setGravity(Gravity.CENTER_HORIZONTAL);
        page.setPadding(0, dp(12), 0, dp(12));

        // Sculpted obsidian remote chassis.
        LinearLayout remote = new LinearLayout(this);
        remote.setOrientation(LinearLayout.VERTICAL);
        remote.setGravity(Gravity.CENTER_HORIZONTAL);
        remote.setPadding(dp(20), dp(16), dp(20), dp(16));
        GradientDrawable remotePanel = new GradientDrawable();
        remotePanel.setColor(color(R.color.background));
        remotePanel.setStroke(dp(1), color(R.color.chassis_outline));
        remotePanel.setCornerRadius(dp(32));
        remote.setBackground(remotePanel);

        // Compact brand header badge.
        LinearLayout header = new LinearLayout(this);
        header.setOrientation(LinearLayout.HORIZONTAL);
        header.setGravity(Gravity.CENTER);
        ImageView logo = new ImageView(this);
        logo.setImageResource(R.drawable.dowelancaster_icon);
        logo.setContentDescription("Dowe LanCaster logo");
        logo.setScaleType(ImageView.ScaleType.CENTER_INSIDE);
        header.addView(logo, margins(dp(28), dp(28), 0, 0, 8, 0));

        LinearLayout brandTextCol = new LinearLayout(this);
        brandTextCol.setOrientation(LinearLayout.VERTICAL);
        TextView title = text("DOWE LANCASTER", 11, R.color.accent);
        title.setTypeface(title.getTypeface(), android.graphics.Typeface.BOLD);
        brandTextCol.addView(title, margins(WRAP, WRAP, 0, 0, 0, 0));
        TextView remoteLabel = text("ROKU REMOTE • ECP READY", 8, R.color.text_secondary);
        brandTextCol.addView(remoteLabel, margins(WRAP, WRAP, 0, 0, 0, 0));
        header.addView(brandTextCol, margins(WRAP, WRAP, 0, 0, 0, 0));
        remote.addView(header, margins(MATCH, WRAP, 0, 0, 0, 12));

        // Recessed system pill bar.
        LinearLayout top = new LinearLayout(this);
        top.setOrientation(LinearLayout.HORIZONTAL);
        top.setGravity(Gravity.CENTER);
        top.setPadding(dp(4), dp(4), dp(4), dp(4));
        GradientDrawable topTrack = new GradientDrawable();
        topTrack.setColor(color(R.color.remote_well));
        topTrack.setStroke(dp(1), color(R.color.chassis_outline));
        topTrack.setCornerRadius(dp(18));
        top.setBackground(topTrack);
        String[][] sysKeys = {
                {"↩ Back", "Back", "surface"},
                {"⌂ Home", "Home", "accent"},
                {"⟳ Replay", "Replay", "surface"},
                {"⏻ Power", "Power", "power"}
        };
        for (String[] sys : sysKeys) {
            int colorRes = sys[2].equals("accent") ? R.color.accent
                    : sys[2].equals("power") ? R.color.power : R.color.surface;
            Button button = remoteButton(sys[0], sys[1], colorRes);
            LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(0, dp(44), 1f);
            params.setMargins(dp(2), 0, dp(2), 0);
            top.addView(button, params);
        }
        remote.addView(top, margins(MATCH, dp(52), 0, 0, 0, 12));

        // Sculpted circular D-pad well and center OK jewel.
        LinearLayout dpad = new LinearLayout(this);
        dpad.setOrientation(LinearLayout.VERTICAL);
        dpad.setGravity(Gravity.CENTER);
        GradientDrawable dpadBackground = new GradientDrawable();
        dpadBackground.setColor(color(R.color.remote_well));
        dpadBackground.setStroke(dp(1), color(R.color.chassis_outline));
        dpadBackground.setCornerRadius(dp(92));
        dpad.setBackground(dpadBackground);
        LinearLayout dpadTop = new LinearLayout(this);
        dpadTop.setGravity(Gravity.CENTER);
        dpadTop.addView(remoteButton("▲", "Up", R.color.surface), new LinearLayout.LayoutParams(dp(64), dp(48)));
        dpad.addView(dpadTop, new LinearLayout.LayoutParams(MATCH, dp(52)));
        LinearLayout dpadMiddle = new LinearLayout(this);
        dpadMiddle.setGravity(Gravity.CENTER);
        dpadMiddle.addView(remoteButton("◀", "Left", R.color.surface), new LinearLayout.LayoutParams(dp(56), dp(52)));
        dpadMiddle.addView(remoteButton("OK", "Select", R.color.accent), margins(dp(68), dp(68), 6, 0, 6, 0));
        dpadMiddle.addView(remoteButton("▶", "Right", R.color.surface), new LinearLayout.LayoutParams(dp(56), dp(52)));
        dpad.addView(dpadMiddle, new LinearLayout.LayoutParams(MATCH, dp(74)));
        LinearLayout dpadBottom = new LinearLayout(this);
        dpadBottom.setGravity(Gravity.CENTER);
        dpadBottom.addView(remoteButton("▼", "Down", R.color.surface), new LinearLayout.LayoutParams(dp(64), dp(48)));
        dpad.addView(dpadBottom, new LinearLayout.LayoutParams(MATCH, dp(52)));
        remote.addView(dpad, margins(dp(184), dp(184), 0, 2, 0, 12));

        // Molded transport and volume control decks.
        String[][] rows = {{"▣|Rev", "▶|Play", "▣|Fwd"}, {"Vol -|VolumeDown", "Mute|Mute", "Vol +|VolumeUp"}};
        for (String[] row : rows) {
            LinearLayout line = new LinearLayout(this);
            line.setGravity(Gravity.CENTER);
            for (String item : row) {
                String[] parts = item.split("\\|", 2);
                String label = parts[0];
                String key = parts[1];
                int buttonColor = key.equals("Select") || key.equals("Play") ? R.color.accent : R.color.surface;
                Button button = remoteButton(label, key, buttonColor);
                LinearLayout.LayoutParams buttonParams = new LinearLayout.LayoutParams(0, dp(44), 1f);
                buttonParams.setMargins(dp(3), dp(3), dp(3), dp(3));
                line.addView(button, buttonParams);
            }
            remote.addView(line, margins(MATCH, dp(50), 0, 0, 0, 4));
        }

        // Inline direct volume set dock.
        LinearLayout volumeDock = new LinearLayout(this);
        volumeDock.setOrientation(LinearLayout.HORIZONTAL);
        volumeDock.setGravity(Gravity.CENTER_VERTICAL);
        EditText volumeInput = input("Roku volume 0-100");
        volumeInput.setInputType(InputType.TYPE_CLASS_NUMBER);
        LinearLayout.LayoutParams volumeInputParams = new LinearLayout.LayoutParams(0, dp(40), 1f);
        volumeInputParams.setMargins(0, 0, dp(6), 0);
        volumeDock.addView(volumeInput, volumeInputParams);
        Button setVolume = remoteButton("Set Volume", "set-volume", R.color.accent);
        setVolume.setOnClickListener(view -> sendValueCommand("set-volume", "value", volumeInput.getText().toString()));
        volumeDock.addView(setVolume, new LinearLayout.LayoutParams(dp(112), dp(40)));
        remote.addView(volumeDock, margins(MATCH, WRAP, 0, 6, 0, 8));

        // Disabled hardware modules.
        LinearLayout disabledAudioRow = new LinearLayout(this);
        disabledAudioRow.setOrientation(LinearLayout.HORIZONTAL);
        disabledAudioRow.setGravity(Gravity.CENTER);
        Button privateListening = remoteButton("♬ Private Listening (Off)", "PrivateListening", R.color.surface);
        privateListening.setEnabled(false);
        privateListening.setAlpha(0.38f);
        LinearLayout.LayoutParams privateParams = new LinearLayout.LayoutParams(0, dp(34), 1f);
        privateParams.setMargins(0, 0, dp(4), 0);
        disabledAudioRow.addView(privateListening, privateParams);
        Button voice = remoteButton("♩ Voice Control (Off)", "VoiceControl", R.color.surface);
        voice.setEnabled(false);
        voice.setAlpha(0.38f);
        LinearLayout.LayoutParams voiceParams = new LinearLayout.LayoutParams(0, dp(34), 1f);
        voiceParams.setMargins(dp(4), 0, 0, 0);
        disabledAudioRow.addView(voice, voiceParams);
        remote.addView(disabledAudioRow, margins(MATCH, dp(36), 0, 0, 0, 10));

        // Recessed keyboard text entry dock.
        TextView keyboardLabel = text("KEYBOARD TEXT DISPATCH", 10, R.color.text_secondary);
        remote.addView(keyboardLabel, margins(MATCH, WRAP, 2, 2, 0, 4));
        LinearLayout textDock = new LinearLayout(this);
        textDock.setOrientation(LinearLayout.HORIZONTAL);
        textDock.setGravity(Gravity.CENTER_VERTICAL);
        EditText textInput = input("Type or dictate text to your Roku...");
        LinearLayout.LayoutParams textInputParams = new LinearLayout.LayoutParams(0, dp(42), 1f);
        textInputParams.setMargins(0, 0, dp(6), 0);
        textDock.addView(textInput, textInputParams);
        Button sendText = remoteButton("Send Text", "remote-text", R.color.accent);
        sendText.setOnClickListener(view -> sendValueCommand("remote-text", "value", textInput.getText().toString()));
        textDock.addView(sendText, new LinearLayout.LayoutParams(dp(104), dp(42)));
        remote.addView(textDock, margins(MATCH, WRAP, 0, 0, 0, 12));

        // Footer navigation.
        Button back = remoteButton("Back to Companion Sections", "back", R.color.surface);
        back.setOnClickListener(view -> showConnectedScreen(page));
        remote.addView(back, margins(MATCH, dp(40), 0, 2, 0, 0));
        page.addView(remote, new LinearLayout.LayoutParams(dp(390), MATCH));
    }

    private Button remoteButton(String label, String key, int tintResource) {
        Button button = new Button(this);
        button.setText(label);
        button.setAllCaps(false);
        button.setTextSize(12);
        button.setTextColor(Color.WHITE);
        button.setMinHeight(0);
        button.setPadding(dp(4), 0, dp(4), 0);
        GradientDrawable buttonBackground = new GradientDrawable();
        buttonBackground.setColor(color(tintResource));
        buttonBackground.setCornerRadius(dp(18));
        button.setBackground(buttonBackground);
        button.setStateListAnimator(null);
        if (!key.equals("set-volume") && !key.equals("remote-text") && !key.equals("back")) {
            button.setOnClickListener(view -> sendRemoteKey(key));
        }
        return button;
    }

    private void showSectionScreen(LinearLayout page, String section) {
        page.removeAllViews();
        TextView title = text(section, 24, R.color.text_primary);
        title.setGravity(Gravity.CENTER_HORIZONTAL);
        page.addView(title, margins(MATCH, WRAP, 0, 18, 0, 12));

        if (section.equals("Link Cast")) {
            EditText url = input("Paste a media link");
            page.addView(url, margins(MATCH, dp(48), 0, 0, 0, 8));
            Button setUrl = remoteButton("Use Link", "set-url", R.color.surface);
            setUrl.setOnClickListener(view -> sendTabAction(section, "set-url", url.getText().toString()));
            page.addView(setUrl, margins(MATCH, dp(42), 0, 0, 0, 8));
            page.addView(actionButton(section, "Analyze Link", "analyze", R.color.accent), margins(MATCH, dp(42), 0, 0, 0, 8));
            page.addView(actionButton(section, "Stream to Roku", "stream", R.color.accent), margins(MATCH, dp(42), 0, 0, 0, 8));
            page.addView(actionButton(section, "Stop Link Stream", "stop", R.color.surface), margins(MATCH, dp(42), 0, 0, 0, 8));
        } else if (section.equals("Live Cast")) {
            page.addView(actionButton(section, "Start Live Cast", "start", R.color.accent), margins(MATCH, dp(46), 0, 0, 0, 8));
            page.addView(actionButton(section, "Stop Live Cast", "stop", R.color.surface), margins(MATCH, dp(46), 0, 0, 0, 8));
        } else if (section.equals("Folder Cast")) {
            page.addView(actionButton(section, "Previous", "previous", R.color.surface), margins(MATCH, dp(42), 0, 0, 0, 8));
            page.addView(actionButton(section, "Play", "play", R.color.accent), margins(MATCH, dp(42), 0, 0, 0, 8));
            page.addView(actionButton(section, "Next", "next", R.color.surface), margins(MATCH, dp(42), 0, 0, 0, 8));
            page.addView(actionButton(section, "Stop", "stop", R.color.surface), margins(MATCH, dp(42), 0, 0, 0, 8));
        } else {
            TextView status = text("This tab is selected on the PC. Its controls are being synchronized through the companion connection.", 15, R.color.text_secondary);
            status.setGravity(Gravity.CENTER_HORIZONTAL);
            status.setTextAlignment(TextView.TEXT_ALIGNMENT_CENTER);
            page.addView(status, margins(MATCH, WRAP, 0, 0, 0, 24));
        }

        Button back = new Button(this);
        back.setText("Back to Companion Sections");
        back.setAllCaps(false);
        back.setOnClickListener(view -> showConnectedScreen(page));
        page.addView(back, margins(MATCH, dp(52), 0, 0, 0, 0));
    }

    private Button actionButton(String tab, String label, String action, int tint) {
        Button button = remoteButton(label, "tab-action", tint);
        button.setOnClickListener(view -> sendTabAction(tab, action, ""));
        return button;
    }

    private void sendTabAction(String tab, String action, String value) {
        if (discoveredEndpoint == null) return;
        networkExecutor.execute(() -> {
            try {
                HttpURLConnection connection = (HttpURLConnection) URI.create("http://" + discoveredEndpoint + "/api/v1/commands/tab-action").toURL().openConnection();
                connection.setRequestMethod("POST");
                connection.setConnectTimeout(5000);
                connection.setReadTimeout(5000);
                connection.setDoOutput(true);
                connection.setRequestProperty("Content-Type", "application/json");
                JSONObject payload = new JSONObject();
                payload.put("tab", tab);
                payload.put("action", action);
                payload.put("value", value);
                try (OutputStream output = connection.getOutputStream()) { output.write(payload.toString().getBytes(StandardCharsets.UTF_8)); }
                connection.getResponseCode();
                connection.disconnect();
            } catch (Exception ignored) { }
        });
    }

    private void loadTabState(String requestedTab, TextView state) {
        if (discoveredEndpoint == null) return;
        networkExecutor.execute(() -> {
            String message;
            try {
                HttpURLConnection connection = (HttpURLConnection)
                        URI.create("http://" + discoveredEndpoint + "/api/v1/state").toURL().openConnection();
                connection.setConnectTimeout(5000);
                connection.setReadTimeout(5000);
                java.io.InputStream input = connection.getInputStream();
                String json = new String(input.readAllBytes(), StandardCharsets.UTF_8);
                JSONObject snapshot = new JSONObject(json);
                JSONObject tabInfo = snapshot.optJSONObject("tabInfo");
                StringBuilder details = new StringBuilder();
                if (tabInfo != null) {
                    details.append(tabInfo.optString("title", requestedTab)).append("\n\n")
                            .append(tabInfo.optString("description", "")).append("\n\nControls available:\n");
                    org.json.JSONArray controls = tabInfo.optJSONArray("controls");
                    if (controls != null) {
                        for (int index = 0; index < controls.length(); index++) {
                            details.append("• ").append(controls.optString(index)).append("\n");
                        }
                    }
                }
                message = "PC active tab: " + snapshot.optString("activeTab", requestedTab)
                        + "\n\n" + details
                        + "\nCompanion service connected on port "
                        + snapshot.optInt("servicePort", 8770) + ".";
                connection.disconnect();
            } catch (Exception exception) {
                message = "The PC tab was selected, but its current state could not be loaded.";
            }
            String finalMessage = message;
            mainHandler.post(() -> state.setText(finalMessage));
        });
    }

    private void sendRemoteKey(String key) {
        sendValueCommand("remote-key", "key", key);
    }

    private void sendValueCommand(String command, String field, String value) {
        if (discoveredEndpoint == null) return;
        networkExecutor.execute(() -> {
            try {
                HttpURLConnection connection = (HttpURLConnection)
                        URI.create("http://" + discoveredEndpoint + "/api/v1/commands/" + command).toURL().openConnection();
                connection.setRequestMethod("POST");
                connection.setConnectTimeout(5000);
                connection.setReadTimeout(5000);
                connection.setDoOutput(true);
                connection.setRequestProperty("Content-Type", "application/json");
                JSONObject payload = new JSONObject();
                payload.put(field, value);
                try (OutputStream output = connection.getOutputStream()) {
                    output.write(payload.toString().getBytes(StandardCharsets.UTF_8));
                }
                connection.getResponseCode();
                connection.disconnect();
            } catch (Exception ignored) { }
        });
    }

    @Override
    protected void onDestroy() {
        networkExecutor.shutdownNow();
        super.onDestroy();
    }

    private EditText input(String hint) {
        EditText field = new EditText(this);
        field.setHint(hint);
        field.setHintTextColor(color(R.color.text_secondary));
        field.setTextColor(color(R.color.text_primary));
        field.setTextSize(16);
        field.setSingleLine(true);
        field.setPadding(dp(14), 0, dp(14), 0);
        GradientDrawable fieldBackground = new GradientDrawable();
        fieldBackground.setColor(color(R.color.surface));
        fieldBackground.setStroke(dp(1), color(R.color.outline));
        fieldBackground.setCornerRadius(dp(8));
        field.setBackground(fieldBackground);
        return field;
    }

    private TextView text(String value, int size, int colorResource) {
        TextView view = new TextView(this);
        view.setText(value);
        view.setTextSize(size);
        view.setTextColor(color(colorResource));
        return view;
    }

    private LinearLayout.LayoutParams margins(int width, int height, int left, int top, int right, int bottom) {
        LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(width, height);
        params.setMargins(dp(left), dp(top), dp(right), dp(bottom));
        return params;
    }

    private int color(int resource) {
        return getColor(resource);
    }

    private int dp(int value) {
        return Math.round(value * getResources().getDisplayMetrics().density);
    }
}
