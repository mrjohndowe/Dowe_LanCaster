using System.Net;

namespace DoweLanCaster.Services;

public static class AirPlayPage
{
    public static string Create(
        string title,
        string mediaUrl,
        string mediaType,
        long completionRevision = 0)
    {
        var safeTitle = WebUtility.HtmlEncode(title);
        var safeUrl = WebUtility.HtmlEncode(mediaUrl);
        var safeType = WebUtility.HtmlEncode(mediaType);
        var completionScript = completionRevision > 0
            ? $"<script>document.querySelector('video').addEventListener('ended',()=>fetch('/control?completedRevision={completionRevision}',{{cache:'no-store'}}));</script>"
            : "";

        return $$"""
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width,initial-scale=1,viewport-fit=cover">
              <meta name="apple-mobile-web-app-capable" content="yes">
              <title>{{safeTitle}} - AirPlay</title>
              <style>
                :root { color-scheme: dark; font-family: -apple-system,BlinkMacSystemFont,"Segoe UI",sans-serif; }
                body { margin:0; min-height:100vh; display:grid; place-items:center; background:#0b1018; color:#f4f7fb; }
                main { width:min(920px,calc(100% - 32px)); }
                h1 { font-size:clamp(24px,5vw,42px); margin:0 0 10px; }
                p { color:#c9d4e2; line-height:1.5; }
                video { width:100%; max-height:70vh; background:#000; border:1px solid #344257; border-radius:14px; }
                .tip { padding:14px 16px; background:#152033; border-radius:10px; }
                .airplay-actions { display:grid; gap:10px; margin-top:14px; }
                .airplay-button {
                  min-height:50px; display:inline-flex; align-items:center; justify-content:center; gap:11px;
                  padding:12px 18px; border:1px solid #69b8ff; border-radius:10px;
                  background:#0c73c9; color:#fff; font:inherit; font-weight:700; font-size:16px;
                  cursor:pointer; box-shadow:0 8px 24px rgba(12,115,201,.25);
                  transition:background-color .16s ease,transform .16s ease,box-shadow .16s ease;
                }
                .airplay-button:hover { background:#095fa8; box-shadow:0 10px 28px rgba(12,115,201,.34); }
                .airplay-button:active { transform:translateY(1px); background:#074d88; }
                .airplay-button:focus-visible { outline:3px solid #b9e1ff; outline-offset:3px; }
                .airplay-mark { position:relative; width:23px; height:17px; border:2px solid currentColor; border-radius:3px; box-sizing:border-box; }
                .airplay-mark::before {
                  content:""; position:absolute; left:50%; bottom:-2px; width:15px; height:3px;
                  background:#0c73c9; transform:translateX(-50%); transition:background-color .16s ease;
                }
                .airplay-mark::after {
                  content:""; position:absolute; left:50%; bottom:-6px; width:0; height:0;
                  border-left:6px solid transparent; border-right:6px solid transparent; border-bottom:8px solid currentColor;
                  transform:translateX(-50%);
                }
                .airplay-button:hover .airplay-mark::before { background:#095fa8; }
                .airplay-button:active .airplay-mark::before { background:#074d88; }
                .airplay-status { margin:0; padding:11px 13px; border-left:3px solid #1687e8; background:#101a29; color:#dbe7f4; }
                .fallback { margin:12px 2px 0; font-size:14px; }
                .fallback a { color:#69b8ff; }
                @media (max-width:600px) {
                  body { align-items:start; }
                  main { width:min(100% - 22px,920px); padding:18px 0; }
                  video { max-height:58vh; border-radius:10px; }
                  .airplay-button { width:100%; }
                }
                @media (prefers-reduced-motion:reduce) { .airplay-button,.airplay-mark::after { transition:none; } }
              </style>
            </head>
            <body>
              <main>
                <h1>{{safeTitle}}</h1>
                <p class="tip">Tap Play, then tap <strong>Choose Roku with AirPlay</strong> and select your Roku. Keep Dowe LanCaster running on the PC while watching.</p>
                <video controls autoplay playsinline x-webkit-airplay="allow">
                  <source src="{{safeUrl}}" type="{{safeType}}">
                  This browser cannot play the prepared stream.
                </video>
                <section class="airplay-actions" aria-label="AirPlay controls">
                  <button id="airplay-picker" class="airplay-button" type="button">
                    <span class="airplay-mark" aria-hidden="true"></span>
                    <span>Choose Roku with AirPlay</span>
                  </button>
                  <p id="airplay-status" class="airplay-status" role="status" aria-live="polite">Checking for AirPlay on this device...</p>
                </section>
                <p class="fallback">If the picker does not open, <a href="{{safeUrl}}">open the video directly</a> in Safari, then use AirPlay from the player.</p>
                <script>
                  (() => {
                    const video = document.querySelector('video');
                    const picker = document.getElementById('airplay-picker');
                    const status = document.getElementById('airplay-status');
                    const supported = typeof video.webkitShowPlaybackTargetPicker === 'function';
                    const unsupportedMessage = 'The AirPlay picker is available in Safari on an iPhone, iPad, or Mac.';

                    status.textContent = supported
                      ? 'Ready. Tap the button to choose your Roku.'
                      : unsupportedMessage;

                    picker.addEventListener('click', () => {
                      if (!supported) {
                        status.textContent = unsupportedMessage;
                        return;
                      }

                      status.textContent = 'Opening the AirPlay device picker...';
                      try {
                        video.webkitShowPlaybackTargetPicker();
                      } catch (error) {
                        status.textContent = 'Safari could not open the AirPlay picker. Make sure Wi-Fi and AirPlay are enabled, then try again.';
                      }
                    });

                    video.addEventListener('webkitplaybacktargetavailabilitychanged', event => {
                      status.textContent = event.availability === 'available'
                        ? 'AirPlay devices are available. Tap the button to choose your Roku.'
                        : 'No AirPlay device is currently visible. Check that the Roku is on the same Wi-Fi network.';
                    });
                  })();
                </script>
                {{completionScript}}
              </main>
            </body>
            </html>
            """;
    }
}
