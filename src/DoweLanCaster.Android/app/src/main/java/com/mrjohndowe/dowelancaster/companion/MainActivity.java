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

        EditText address = input("PC IP:PORT, for example 10.0.0.25:8770");
        address.setInputType(InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_VARIATION_URI);
        page.addView(address, margins(MATCH, dp(54), 0, 0, 0, 12));

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

        pair.setOnClickListener(view -> pairWithPc(address.getText().toString(), code.getText().toString(), pair, status));

        setContentView(page);
    }

    private void pairWithPc(String addressText, String codeText, Button pairButton, TextView status) {
        String endpoint = addressText.trim();
        String pairingCode = codeText.trim();
        if (!endpoint.matches("^[^\\s/:]+:[1-9][0-9]{0,4}$") || pairingCode.length() != 6) {
            status.setText("Enter the PC IP:PORT and six-digit pairing code.");
            return;
        }

        pairButton.setEnabled(false);
        status.setText("Connecting to the PC companion service...");
        networkExecutor.execute(() -> {
            String result;
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
                result = responseCode >= 200 && responseCode < 300
                        ? "Paired with Dowe LanCaster on the PC."
                        : "The PC rejected the pairing code (HTTP " + responseCode + ").";
                connection.disconnect();
            } catch (Exception exception) {
                result = "Could not reach the PC. Check the IP:PORT and that the companion service is running.";
            }

            String finalResult = result;
            mainHandler.post(() -> {
                status.setText(finalResult);
                pairButton.setEnabled(true);
                Toast.makeText(this, finalResult, Toast.LENGTH_SHORT).show();
            });
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
