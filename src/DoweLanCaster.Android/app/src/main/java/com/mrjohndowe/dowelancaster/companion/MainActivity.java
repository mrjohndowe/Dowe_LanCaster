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

        EditText code = input("One-time pairing code");
        code.setInputType(InputType.TYPE_CLASS_NUMBER | InputType.TYPE_NUMBER_VARIATION_PASSWORD);
        page.addView(code, margins(MATCH, dp(54), 0, 0, 0, 16));

        Button pair = new Button(this);
        pair.setText("Pair with PC");
        pair.setTextSize(16);
        pair.setAllCaps(false);
        pair.setTextColor(Color.WHITE);
        pair.setBackgroundTintList(getColorStateList(R.color.accent));
        page.addView(pair, margins(MATCH, dp(54), 0, 0, 0, 20));

        TextView status = text(
                "Enter the PC IP:PORT and one-time code shown in Dowe LanCaster Settings.",
                14, R.color.text_secondary);
        status.setGravity(Gravity.CENTER_HORIZONTAL);
        page.addView(status, margins(MATCH, WRAP, 0, 0, 0, 0));

        pair.setOnClickListener(view -> pairWithPc(page, code.getText().toString(), pair, status));

        setContentView(page);
        discoverPc(discovery, status, pair);
    }

    private void discoverPc(TextView discovery, TextView status, Button pairButton) {
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
                pairButton.setEnabled(foundEndpoint != null);
                if (foundEndpoint == null) {
                    status.setText("Start the companion service on the PC, then try again.");
                }
            });
        });
    }

    private void pairWithPc(LinearLayout page, String codeText, Button pairButton, TextView status) {
        String endpoint = discoveredEndpoint;
        String pairingCode = codeText.trim();
        if (endpoint == null || pairingCode.length() != 6) {
            status.setText("Enter the six-digit pairing code shown on the PC.");
            return;
        }

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

                URI uri = URI.create("http://" + endpoint + "/api/v1/pairing/approve");
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
        LinearLayout remote = new LinearLayout(this);
        remote.setOrientation(LinearLayout.VERTICAL);
        remote.setPadding(dp(18), dp(12), dp(18), dp(12));
        GradientDrawable remotePanel = new GradientDrawable();
        remotePanel.setColor(color(R.color.background));
        remotePanel.setStroke(dp(1), color(R.color.outline));
        remotePanel.setCornerRadius(dp(24));
        remote.setBackground(remotePanel);
        ImageView logo = new ImageView(this);
        logo.setImageResource(R.drawable.dowelancaster_icon);
        logo.setContentDescription("Dowe LanCaster logo");
        logo.setScaleType(ImageView.ScaleType.CENTER_INSIDE);
        remote.addView(logo, margins(MATCH, dp(34), 0, 0, 0, 2));

        TextView title = text("DOWE LANCASTER", 11, R.color.accent);
        title.setGravity(Gravity.CENTER_HORIZONTAL);
        title.setTypeface(title.getTypeface(), android.graphics.Typeface.BOLD);
        remote.addView(title, margins(MATCH, WRAP, 0, 0, 0, 1));
        TextView remoteLabel = text("ROKU REMOTE", 8, R.color.text_secondary);
        remoteLabel.setGravity(Gravity.CENTER_HORIZONTAL);
        remote.addView(remoteLabel, margins(MATCH, WRAP, 0, 0, 0, 4));

        Button popout = remoteButton("Open Pop-out Remote", "popout", R.color.accent);
        popout.setOnClickListener(view -> Toast.makeText(this, "Open the pop-out remote from the PC remote section.", Toast.LENGTH_SHORT).show());
        remote.addView(popout, margins(MATCH, dp(34), 0, 0, 0, 8));

        LinearLayout top = new LinearLayout(this);
        top.setGravity(Gravity.CENTER);
        top.addView(remoteButton("↩ Back", "Back", R.color.surface), new LinearLayout.LayoutParams(0, dp(42), 1));
        top.addView(remoteButton("⌂ Home", "Home", R.color.accent), new LinearLayout.LayoutParams(0, dp(42), 1));
        top.addView(remoteButton("⟳ Replay", "Replay", R.color.surface), new LinearLayout.LayoutParams(0, dp(42), 1));
        top.addView(remoteButton("⏻ Power", "Power", R.color.power), new LinearLayout.LayoutParams(0, dp(42), 1));
        remote.addView(top, margins(MATCH, dp(42), 0, 0, 0, 8));

        String[][] rows = {{"▲|Up"}, {"◀|Left", "OK|Select", "▶|Right"}, {"▼|Down"}, {"▣|Rev", "▶|Play", "▣|Fwd"}, {"Vol -|VolumeDown", "Mute|Mute", "Vol +|VolumeUp"}};
        for (String[] row : rows) {
            LinearLayout line = new LinearLayout(this);
            line.setGravity(Gravity.CENTER);
            for (String item : row) {
                String[] parts = item.split("\\|", 2);
                String label = parts[0];
                String key = parts[1];
                Button button = remoteButton(label, key, key.equals("Select") ? R.color.accent : R.color.surface);
                LinearLayout.LayoutParams buttonParams = new LinearLayout.LayoutParams(0, dp(42), 1);
                buttonParams.setMargins(dp(3), dp(3), dp(3), dp(3));
                line.addView(button, buttonParams);
            }
            remote.addView(line, margins(MATCH, dp(48), 0, 0, 0, 0));
        }

        EditText volumeInput = input("Roku volume 0-100");
        volumeInput.setInputType(InputType.TYPE_CLASS_NUMBER);
        remote.addView(volumeInput, margins(MATCH, dp(38), 0, 6, 0, 4));
        Button setVolume = remoteButton("Set Volume", "set-volume", R.color.accent);
        setVolume.setOnClickListener(view -> sendValueCommand("set-volume", "value", volumeInput.getText().toString()));
        remote.addView(setVolume, margins(MATCH, dp(36), 0, 0, 0, 6));

        Button privateListening = remoteButton("♬ Start Roku Private Listening", "PrivateListening", R.color.surface);
        privateListening.setOnClickListener(view -> sendRemoteKey("PrivateListening"));
        remote.addView(privateListening, margins(MATCH, dp(36), 0, 0, 0, 2));
        TextView privateHint = text("Private listening status is shown on the PC remote.", 8, R.color.text_secondary);
        privateHint.setGravity(Gravity.CENTER_HORIZONTAL);
        remote.addView(privateHint, margins(MATCH, WRAP, 0, 0, 0, 6));

        Button voice = remoteButton("♩ Start Voice Control", "VoiceControl", R.color.accent);
        voice.setOnClickListener(view -> sendRemoteKey("VoiceControl"));
        remote.addView(voice, margins(MATCH, dp(36), 0, 0, 0, 2));
        TextView voiceHint = text("Voice control is off", 8, R.color.text_secondary);
        voiceHint.setGravity(Gravity.CENTER_HORIZONTAL);
        remote.addView(voiceHint, margins(MATCH, WRAP, 0, 0, 0, 6));

        TextView keyboardLabel = text("Keyboard Text", 12, R.color.text_primary);
        remote.addView(keyboardLabel, margins(MATCH, WRAP, 0, 4, 0, 2));
        EditText textInput = input("Type or dictate text to your Roku...");
        remote.addView(textInput, margins(MATCH, dp(42), 0, 6, 0, 4));
        Button sendText = remoteButton("Send Text to Roku", "remote-text", R.color.accent);
        sendText.setOnClickListener(view -> sendValueCommand("remote-text", "value", textInput.getText().toString()));
        remote.addView(sendText, margins(MATCH, dp(36), 0, 0, 0, 6));

        Button back = remoteButton("Back to Companion Sections", "back", R.color.surface);
        back.setOnClickListener(view -> showConnectedScreen(page));
        remote.addView(back, margins(MATCH, dp(36), 0, 6, 0, 0));
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
