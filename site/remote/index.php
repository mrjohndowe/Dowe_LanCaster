<?php

declare(strict_types=1);

// The PHP endpoint remains responsible for Roku requests. The remote interface
// is the compiled React app, which already calls that endpoint directly.
// Do not inject a second click handler here: it duplicates commands and can
// leave the browser's blocking alert open over the remote controls.
$appPath = __DIR__ . '/dist/index.html';

if (!is_file($appPath)) {
    http_response_code(503);
    header('Content-Type: text/plain; charset=utf-8');
    echo 'The remote interface has not been built yet.';
    exit;
}

$html = file_get_contents($appPath);
if ($html === false) {
    http_response_code(500);
    header('Content-Type: text/plain; charset=utf-8');
    echo 'Could not load the remote interface.';
    exit;
}

// Vite emits paths relative to dist/index.html. This PHP page is served one
// level above dist, so keep the generated asset URLs pointing at dist/assets.
$html = str_replace('./assets/', 'dist/assets/', $html);

header('Content-Type: text/html; charset=utf-8');
header('Cache-Control: no-store');
echo $html;
