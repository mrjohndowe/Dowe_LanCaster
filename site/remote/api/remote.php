<?php

declare(strict_types=1);
header('Content-Type: application/json; charset=utf-8');
header('Cache-Control: no-store');
function reply(array $body, int $status = 200): never
{
    http_response_code($status);
    $jsonCode = json_encode($body, JSON_UNESCAPED_SLASHES);
    echo $jsonCode;


    exit;
}
function httpCall(string $url, string $method = 'GET', string $body = ''): array
{
    $context = stream_context_create(['http' => ['method' => $method, 'header' => "Content-Type: application/x-www-form-urlencoded\r\nAccept: application/json\r\n", 'content' => $body, 'timeout' => 3, 'ignore_errors' => true]]);
    $response = @file_get_contents($url, false, $context);
    $status = 0;
    $responseHeaders = function_exists('http_get_last_response_headers')
        ? http_get_last_response_headers()
        : ($http_response_header ?? []);
    if (!empty($responseHeaders[0]) && preg_match('/\s(\d{3})\s/', $responseHeaders[0], $m)) $status = (int)$m[1];
    return [$status, is_string($response) ? $response : ''];
}
function companionCall(string $action): array
{
    $context = stream_context_create(['http' => [
        'method' => 'POST',
        'header' => "Content-Type: application/json\r\nAccept: application/json\r\n",
        'content' => json_encode(['value' => $action], JSON_UNESCAPED_SLASHES),
        'timeout' => 3,
        'ignore_errors' => true
    ]]);
    $response = @file_get_contents('http://127.0.0.1:8770/api/v1/commands/remote-action', false, $context);
    $responseHeaders = function_exists('http_get_last_response_headers')
        ? http_get_last_response_headers()
        : ($http_response_header ?? []);
    $status = !empty($responseHeaders[0]) && preg_match('/\s(\d{3})\s/', $responseHeaders[0], $match)
        ? (int)$match[1]
        : 0;
    return [$status, is_string($response) ? $response : ''];
}
try {
    $sessionPath = sys_get_temp_dir() . DIRECTORY_SEPARATOR . 'dowe-lancaster-remote-sessions';
    if (!is_dir($sessionPath)) @mkdir($sessionPath, 0700, true);
    if (is_writable($sessionPath)) session_save_path($sessionPath);
    session_start();
    function rokuEndpoint(string $ip): ?string
    {
        $ip = trim($ip);
        if (!filter_var($ip, FILTER_VALIDATE_IP, FILTER_FLAG_IPV4)) return null;
        [$status] = httpCall("http://$ip:8060/query/device-info");
        return $status === 200 ? "http://$ip:8060" : null;
    }
    function discoverRoku(): array
    {
        $context = stream_context_create(['socket' => ['so_broadcast' => true]]);
        $socket = @stream_socket_server('udp://0.0.0.0:0', $errno, $error, STREAM_SERVER_BIND, $context);
        if (!is_resource($socket)) return [];
        try {
            $search = "M-SEARCH * HTTP/1.1\r\n"
                . "HOST: 239.255.255.250:1900\r\n"
                . "MAN: \"ssdp:discover\"\r\n"
                . "ST: roku:ecp\r\n"
                . "MX: 2\r\n\r\n";
            @stream_socket_sendto($socket, $search, 0, '239.255.255.250:1900');

            $deadline = microtime(true) + 2.5;
            $endpoints = [];
            while (($remaining = $deadline - microtime(true)) > 0) {
                $read = [$socket]; $write = null; $except = null;
                $seconds = (int)$remaining;
                $microseconds = (int)(($remaining - $seconds) * 1_000_000);
                if (@stream_select($read, $write, $except, $seconds, $microseconds) !== 1) break;
                $peer = '';
                $reply = @stream_socket_recvfrom($socket, 128, 0, $peer);
                if (is_string($reply) && preg_match('/^location:\s*http:\/\/([^:\/]+):8060\//im', $reply, $m)) {
                    $endpoints['http://' . $m[1] . ':8060'] = true;
                }
            }

            $devices = [];
            foreach (array_keys($endpoints) as $endpoint) {
                [$status, $deviceInfo] = httpCall($endpoint . '/query/device-info');
                if ($status !== 200) continue;

                $name = $endpoint;
                if (preg_match('/<friendly-device-name>(.*?)<\/friendly-device-name>/is', $deviceInfo, $match)) {
                    $name = html_entity_decode(trim($match[1]), ENT_QUOTES | ENT_XML1, 'UTF-8');
                }
                $devices[] = ['endpoint' => $endpoint, 'name' => $name];
            }
            return $devices;
        } finally {
            fclose($socket);
        }
    }
    if (($_SERVER['REQUEST_METHOD'] ?? 'GET') !== 'POST') reply(['ok' => false, 'message' => 'POST only.'], 405);
    $data = json_decode((string)file_get_contents('php://input'), true) ?: [];
    $action = (string)($data['action'] ?? '');
    if ($action === 'status') reply(['ok' => true, 'connected' => isset($_SESSION['roku_endpoint']), 'roku' => $_SESSION['roku_name'] ?? null]);
    if ($action === 'disconnect') {
        unset($_SESSION['roku_endpoint'], $_SESSION['roku_name']);
        reply(['ok' => true, 'connected' => false]);
    }
    if ($action === 'discover') reply(['ok' => true, 'devices' => discoverRoku()]);
    if ($action === 'connect') {
        $ip = (string)($data['ip'] ?? '');
        if ($ip === 'discover') {
            $devices = discoverRoku();
            $device = $devices[0] ?? null;
        } else {
            $endpoint = rokuEndpoint($ip);
            $device = $endpoint ? ['endpoint' => $endpoint, 'name' => $endpoint] : null;
        }
        if (!$device) reply(['ok' => false, 'message' => 'No Roku device responded. Discover Roku devices or enter the Roku IP address.'], 404);
        $_SESSION['roku_endpoint'] = $device['endpoint'];
        $_SESSION['roku_name'] = $device['name'];
        reply(['ok' => true, 'connected' => true, 'roku' => $device['name']]);
    }
    if (empty($_SESSION['roku_endpoint'])) reply(['ok' => false, 'message' => 'Connect to a Roku before sending commands.'], 401);
    $endpoint = $_SESSION['roku_endpoint'];
    $command = (string)($data['command'] ?? '');
    $keys = ['power' => 'Power', 'home' => 'Home', 'back' => 'Back', 'up' => 'Up', 'down' => 'Down', 'left' => 'Left', 'right' => 'Right', 'select' => 'Select', 'replay' => 'InstantReplay', 'play_pause' => 'Play', 'rev' => 'Rev', 'fwd' => 'Fwd', 'volume_down' => 'VolumeDown', 'volume_up' => 'VolumeUp', 'mute' => 'VolumeMute'];
    if (isset($keys[$command])) [$status] = httpCall($endpoint . '/keypress/' . $keys[$command], 'POST');
    elseif ($command === 'text' && is_string($data['value'] ?? null) && trim($data['value']) !== '') [$status] = httpCall($endpoint . '/input', 'POST', http_build_query(['text' => $data['value']]));
    elseif ($command === 'volume') {
        $level = filter_var($data['value'] ?? null, FILTER_VALIDATE_INT);
        if ($level === false || $level < 0 || $level > 100)
            reply(['ok' => false, 'message' => 'Enter a volume from 0 to 100.'], 422);
        [$audioStatus, $audioXml] = httpCall($endpoint . '/query/audio-device');
        if ($audioStatus !== 200 || !preg_match('/<global[^>]*>.*?<volume>(\\d+)<\\/volume>/is', $audioXml, $match))
            reply(['ok' => false, 'message' => 'Roku did not report its current volume.'], 502);
        $current = (int)$match[1];
        $key = $level < $current ? 'VolumeDown' : 'VolumeUp';
        $status = 200;
        for ($i = 0; $i < abs($level - $current); $i++) {
            [$status] = httpCall($endpoint . '/keypress/' . $key, 'POST');
            if ($status < 200 || $status >= 300) break;
        }
    }
    elseif ($command === 'private_listening' || $command === 'voice_control') {
        [$status] = companionCall($command === 'private_listening' ? 'private-listening' : 'voice-control');
        if ($status < 200 || $status >= 300)
            reply(['ok' => false, 'message' => 'The Dowe LanCaster desktop app is not ready. Open the updated app on this PC and try again.'], 503);
        reply(['ok' => true, 'message' => $command === 'private_listening' ? 'Private Listening was toggled in Dowe LanCaster.' : 'Voice Control was toggled in Dowe LanCaster.']);
    }
    else reply(['ok' => false, 'message' => 'Unsupported Roku command: ' . $command], 422);
    if (($status ?? 0) < 200 || ($status ?? 0) >= 300) reply(['ok' => false, 'message' => 'Roku did not confirm that command. Status: ' . ($status ?? 'unknown')], 502);
    reply(['ok' => true, 'message' => 'Roku command sent.']);
} catch (Throwable $error) {
    reply(['ok' => false, 'message' => 'Roku remote failed safely.'], 500);
}
