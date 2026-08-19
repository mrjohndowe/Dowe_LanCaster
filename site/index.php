<?php
declare(strict_types=1);

$release = [
    'version' => '0.7.0',
    'windows_file' => '../dist/DoweLanCaster-Windows-x64.zip',
    'roku_file' => '../dist/DoweLanCaster-Roku.zip',
];

function fileSizeLabel(string $path): string
{
    if (!is_file($path)) {
        return 'Build locally';
    }

    $bytes = filesize($path);
    if ($bytes === false) {
        return 'Download';
    }

    return $bytes >= 1048576
        ? number_format($bytes / 1048576, 1) . ' MB'
        : number_format($bytes / 1024, 0) . ' KB';
}

function downloadAvailable(string $path): bool
{
    return is_file($path);
}
?>
<!doctype html>
<html lang="en">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <meta name="description" content="Dowe LanCaster casts video, folders, links, and your Windows screen to Roku over your local network.">
    <meta name="theme-color" content="#071016">
    <title>Dowe LanCaster <?= htmlspecialchars($release['version']) ?> — Cast Windows to Roku</title>
    <link rel="icon" href="../DoweLanCaster.ico">
    <link rel="stylesheet" href="assets/site.css">
    <script src="assets/site.js" defer></script>
</head>
<body>
    <div class="ambient ambient-one" aria-hidden="true"></div>
    <div class="ambient ambient-two" aria-hidden="true"></div>

    <header class="site-header">
        <a class="brand" href="#top" aria-label="Dowe LanCaster home">
            <img src="../logo.png" alt="" width="44" height="44">
            <span>Dowe <strong>LanCaster</strong></span>
        </a>
        <button class="nav-toggle" type="button" aria-expanded="false" aria-controls="site-nav">
            <span></span><span></span>
            <span class="sr-only">Open navigation</span>
        </button>
        <nav id="site-nav" aria-label="Main navigation">
            <a href="#features">Features</a>
            <a href="#how-it-works">How it works</a>
            <a href="#release">What’s new</a>
            <a class="nav-download" href="#download">Get v<?= htmlspecialchars($release['version']) ?></a>
        </nav>
    </header>

    <main id="top">
        <section class="hero section-shell">
            <div class="hero-copy reveal">
                <div class="eyebrow"><span></span> Windows to Roku · Local network</div>
                <h1>Your screen.<br>Your media.<br><em>Your Roku.</em></h1>
                <p class="hero-lede">Cast videos, complete folders, public media links, or your live Windows desktop straight to Roku—without sending your content through the cloud.</p>
                <div class="hero-actions">
                    <a class="button button-primary" href="#download">Download v<?= htmlspecialchars($release['version']) ?><span aria-hidden="true">↓</span></a>
                    <button class="button button-quiet" type="button" data-play-video><span class="play-icon" aria-hidden="true">▶</span> Watch the intro</button>
                </div>
                <div class="hero-meta">
                    <span>Windows 10/11</span>
                    <span>Roku receiver included</span>
                    <span>LAN-first streaming</span>
                </div>
            </div>

            <div class="hero-visual reveal delay-one">
                <div class="signal-ring ring-one"></div>
                <div class="signal-ring ring-two"></div>
                <div class="device-card">
                    <div class="device-bar"><i></i><i></i><i></i><span>LIVE ON YOUR LAN</span></div>
                    <video id="intro-video" controls preload="metadata" poster="../DoweLanCaster-intro-v0.7.0.png">
                        <source src="../src/DoweLanCaster.Windows/Resources/intro.mp4" type="video/mp4">
                        Your browser does not support embedded MP4 video.
                    </video>
                    <div class="device-status">
                        <div><small>NOW CASTING</small><strong>Dowe LanCaster</strong></div>
                        <span class="live-pill"><i></i> Connected</span>
                    </div>
                </div>
            </div>
        </section>

        <section class="ticker" aria-label="Supported capabilities">
            <div class="ticker-track">
                <span>Folder playlists</span><i>◆</i><span>Live desktop</span><i>◆</i><span>Public media links</span><i>◆</i><span>Local files</span><i>◆</i><span>System audio</span><i>◆</i><span>Roku remote</span>
            </div>
        </section>

        <section class="features section-shell" id="features">
            <div class="section-heading reveal">
                <div class="eyebrow"><span></span> One app, every way to cast</div>
                <h2>Pick the source.<br><em>LanCaster handles the rest.</em></h2>
                <p>Hardware-accelerated H.264 encoding, low-latency HLS, and automatic Roku discovery keep setup out of your way.</p>
            </div>

            <div class="feature-grid">
                <article class="feature-card feature-wide reveal">
                    <div class="feature-number">01</div>
                    <div class="feature-icon folder-icon" aria-hidden="true"><span></span></div>
                    <h3>Folder Cast</h3>
                    <p>Turn an entire folder into a Roku playlist. Reorder, shuffle, repeat, include subfolders, and automatically skip failed items.</p>
                    <ul>
                        <li>Auto-play next</li><li>Mixed formats</li><li>Saved preferences</li>
                    </ul>
                </article>
                <article class="feature-card reveal delay-one">
                    <div class="feature-number">02</div>
                    <div class="feature-icon link-icon" aria-hidden="true">↗</div>
                    <h3>Link Cast</h3>
                    <p>Paste a supported public, non-DRM media page or direct video URL and stream it to Roku.</p>
                </article>
                <article class="feature-card reveal delay-two">
                    <div class="feature-number">03</div>
                    <div class="feature-icon screen-icon" aria-hidden="true"><span></span></div>
                    <h3>Live Cast</h3>
                    <p>Share your full desktop, a monitor, or one application window—with optional system audio.</p>
                </article>
                <article class="feature-card reveal">
                    <div class="feature-number">04</div>
                    <div class="feature-icon file-icon" aria-hidden="true"><span></span></div>
                    <h3>Local File Cast</h3>
                    <p>Play local media through a Roku-friendly stream, with FFmpeg conversion when needed.</p>
                </article>
                <article class="feature-card feature-wide reveal delay-one">
                    <div class="feature-number">05</div>
                    <div class="remote-layout">
                        <div>
                            <div class="feature-icon remote-icon" aria-hidden="true">⌁</div>
                            <h3>Discovery & remote</h3>
                            <p>Find Roku devices over SSDP, add one by IP, launch installed channels, and control playback from the same Windows app.</p>
                        </div>
                        <div class="remote-pad" aria-hidden="true"><b>⌃</b><b>‹</b><b>●</b><b>›</b><b>⌄</b></div>
                    </div>
                </article>
            </div>
        </section>

        <section class="how section-shell" id="how-it-works">
            <div class="how-panel reveal">
                <div class="section-heading compact">
                    <div class="eyebrow"><span></span> From PC to TV</div>
                    <h2>Three steps.<br><em>One local connection.</em></h2>
                </div>
                <ol class="steps">
                    <li><span>1</span><div><strong>Sideload the receiver</strong><p>Install the included Roku channel package using Roku’s developer mode.</p></div></li>
                    <li><span>2</span><div><strong>Choose your Roku</strong><p>Let LanCaster discover it automatically, or enter its local IP address.</p></div></li>
                    <li><span>3</span><div><strong>Choose what to cast</strong><p>Select a file, folder, link, monitor, desktop, or application—and press play.</p></div></li>
                </ol>
            </div>
            <div class="network-graphic reveal delay-one" aria-label="Windows PC streams over local Wi-Fi to Roku TV">
                <div class="network-node pc-node"><span>▰</span><small>WINDOWS PC</small></div>
                <div class="network-line"><i></i><b>LOCAL WI-FI</b></div>
                <div class="network-node tv-node"><span>▶</span><small>ROKU TV</small></div>
            </div>
        </section>

        <section class="release section-shell" id="release">
            <div class="version-mark reveal"><small>RELEASE</small><strong><?= htmlspecialchars($release['version']) ?></strong></div>
            <div class="release-copy reveal delay-one">
                <div class="eyebrow"><span></span> Latest release</div>
                <h2>Folder Cast has arrived.</h2>
                <p>Version <?= htmlspecialchars($release['version']) ?> adds complete-folder playlists, recursive scanning, mixed-format transcoding, shuffle and repeat modes, playlist reordering, automatic next-play, and remembered settings.</p>
                <div class="format-list" aria-label="Supported Folder Cast formats">
                    <span>MP4</span><span>MKV</span><span>AVI</span><span>WEBM</span><span>MOV</span><span>MPEG</span><span>TS</span><span>WMV</span><span>FLV</span>
                </div>
            </div>
        </section>

        <section class="download section-shell" id="download">
            <div class="download-card reveal">
                <div>
                    <div class="eyebrow light"><span></span> Ready when you are</div>
                    <h2>Put your media<br>on the big screen.</h2>
                    <p>Dowe LanCaster <?= htmlspecialchars($release['version']) ?> for Windows x64. The Roku receiver is packaged separately for sideloading.</p>
                </div>
                <div class="download-actions">
                    <?php if (downloadAvailable($release['windows_file'])): ?>
                        <a class="download-row primary-download" href="<?= htmlspecialchars($release['windows_file']) ?>" download>
                            <span><strong>Windows app</strong><small>ZIP · <?= fileSizeLabel($release['windows_file']) ?></small></span><b>↓</b>
                        </a>
                    <?php else: ?>
                        <div class="download-row unavailable">
                            <span><strong>Windows app</strong><small>Run BUILD-RELEASE.cmd first</small></span><b>—</b>
                        </div>
                    <?php endif; ?>
                    <?php if (downloadAvailable($release['roku_file'])): ?>
                        <a class="download-row" href="<?= htmlspecialchars($release['roku_file']) ?>" download>
                            <span><strong>Roku receiver</strong><small>ZIP · <?= fileSizeLabel($release['roku_file']) ?></small></span><b>↓</b>
                        </a>
                    <?php endif; ?>
                    <p class="fine-print">Requires Windows 10/11, .NET 8, a Roku device, and both devices on the same private network. Public link casting does not bypass DRM, paywalls, or protected playback.</p>
                </div>
            </div>
        </section>
    </main>

    <footer class="site-footer section-shell">
        <a class="brand" href="#top"><img src="../logo.png" alt="" width="36" height="36"><span>Dowe <strong>LanCaster</strong></span></a>
        <p>Built for the living room. Streamed on your LAN.</p>
        <span>Version <?= htmlspecialchars($release['version']) ?></span>
    </footer>
</body>
</html>
