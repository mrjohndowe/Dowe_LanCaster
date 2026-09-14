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
              </style>
            </head>
            <body>
              <main>
                <h1>{{safeTitle}}</h1>
                <p class="tip">Tap Play, then tap the AirPlay icon and choose your Roku. Keep Dowe LanCaster running on the PC while watching.</p>
                <video controls autoplay playsinline x-webkit-airplay="allow">
                  <source src="{{safeUrl}}" type="{{safeType}}">
                  This browser cannot play the prepared stream.
                </video>
                {{completionScript}}
              </main>
            </body>
            </html>
            """;
    }
}
