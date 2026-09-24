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
        TextView title = text("Dowe LanCaster Remote", 24, R.color.text_primary);
        title.setGravity(Gravity.CENTER_HORIZONTAL);
        page.addView(title, margins(MATCH, WRAP, 0, 16, 0, 20));
        String[][] rows = {{"Home", "Back"}, {"Up", "Select", "Down"}, {"Left", "Right"}, {"Rev", "Play", "Fwd"}, {"VolumeDown", "Mute", "VolumeUp"}, {"Power"}};
        for (String[] row : rows) {
            LinearLayout line = new LinearLayout(this);
            line.setGravity(Gravity.CENTER);
            for (String key : row) {
                Button button = new Button(this);
                button.setText(key.equals("Select") ? "OK" : key);
                button.setAllCaps(false);
                button.setOnClickListener(view -> sendRemoteKey(key));
                line.addView(button, new LinearLayout.LayoutParams(0, dp(54), 1));
            }
            page.addView(line, margins(MATCH, dp(58), 0, 0, 0, 6));
        }

        EditText textInput = input("Type text for the Roku");
        page.addView(textInput, margins(MATCH, dp(54), 0, 14, 0, 8));
        Button sendText = new Button(this);
        sendText.setText("Send Text to Roku");
        sendText.setAllCaps(false);
        sendText.setOnClickListener(view -> sendValueCommand("remote-text", "value", textInput.getText().toString()));
        page.addView(sendText, margins(MATCH, dp(52), 0, 0, 0, 8));

        EditText volumeInput = input("Roku volume 0-100");
        volumeInput.setInputType(InputType.TYPE_CLASS_NUMBER);
        page.addView(volumeInput, margins(MATCH, dp(54), 0, 8, 0, 8));
        Button setVolume = new Button(this);
        setVolume.setText("Set Roku Volume");
        setVolume.setAllCaps(false);
        setVolume.setOnClickListener(view -> sendValueCommand("set-volume", "value", volumeInput.getText().toString()));
        page.addView(setVolume, margins(MATCH, dp(52), 0, 0, 0, 8));
        Button back = new Button(this);
        back.setText("Back to Companion Sections");
        back.setOnClickListener(view -> showConnectedScreen(page));
        page.addView(back, margins(MATCH, dp(52), 0, 16, 0, 0));
    }

    private void showSectionScreen(LinearLayout page, String section) {
        page.removeAllViews();
        TextView title = text(section, 26, R.color.text_primary);
        title.setGravity(Gravity.CENTER_HORIZONTAL);
        page.addView(title, margins(MATCH, WRAP, 0, 18, 0, 12));

        TextView state = text("Loading the live PC tab information...", 16, R.color.text_secondary);
        state.setGravity(Gravity.CENTER_HORIZONTAL);
        state.setTextAlignment(TextView.TEXT_ALIGNMENT_CENTER);
        page.addView(state, margins(MATCH, WRAP, 0, 0, 0, 24));

        TextView information = text("This tab is active on the PC. Its controls will stay synchronized with the PC companion service.", 16, R.color.text_secondary);
        information.setGravity(Gravity.CENTER_HORIZONTAL);
        information.setTextAlignment(TextView.TEXT_ALIGNMENT_CENTER);
        page.addView(information, margins(MATCH, WRAP, 0, 0, 0, 24));

        Button back = new Button(this);
        back.setText("Back to Companion Sections");
        back.setAllCaps(false);
        back.setOnClickListener(view -> showConnectedScreen(page));
        page.addView(back, margins(MATCH, dp(52), 0, 0, 0, 0));

        loadTabState(section, state);
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
                message = "PC active tab: " + snapshot.optString("activeTab", requestedTab)
                        + "\n\nCompanion service connected on port "
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
        field.setBackgroundTintList(getColorStateList(R.color.outline));
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
