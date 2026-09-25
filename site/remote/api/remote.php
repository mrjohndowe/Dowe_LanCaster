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
    if (!empty($http_response_header[0]) && preg_match('/\s(\d{3})\s/', $http_response_header[0], $m)) $status = (int)$m[1];
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
        if (!is_resource($socket)) return null;
        try {
            $targets = ['255.255.255.255'];
            $localAddresses = @gethostbynamel(gethostname()) ?: [];
            foreach ($localAddresses as $localAddress) {
                if (filter_var($localAddress, FILTER_VALIDATE_IP, FILTER_FLAG_IPV4)) {
                    $parts = explode('.', $localAddress);
                    if (count($parts) === 4) $targets[] = "$parts[0].$parts[1].$parts[2].255";
                }
            }
            foreach (array_unique($targets) as $target) {
                @stream_socket_sendto($socket, 'DOWE_LANCASTER_DISCOVER', 0, "$target:8771");
            }
            $deadline = microtime(true) + 2.5;
            $candidates = [];
            while (($remaining = $deadline - microtime(true)) > 0) {
                $read = [$socket]; $write = null; $except = null;
                $seconds = (int)$remaining;
                $microseconds = (int)(($remaining - $seconds) * 1_000_000);
                if (@stream_select($read, $write, $except, $seconds, $microseconds) !== 1) break;
                $peer = '';
                $reply = @stream_socket_recvfrom($socket, 128, 0, $peer);
                if (is_string($reply) && preg_match('/^DOWE_LANCASTER_PC\\|(\\d{1,5})$/', trim($reply), $m) && preg_match('/^([^:]+):\\d+$/', $peer, $ip)) {
                    $candidates[] = 'http://' . $ip[1] . ':' . $m[1];
                }
            }
            foreach (array_unique($candidates) as $candidate) {
                $health = @file_get_contents($candidate . '/api/v1/health', false, stream_context_create(['http' => ['timeout' => 2]]));
                if (is_string($health) && $health !== '') return $candidate;
            }
            return null;
        } finally {
            fclose($socket);
        }
        return array_values($found);
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
    elseif ($command === 'volume') reply(['ok' => false, 'message' => 'Use Volume Up, Volume Down, or Mute with this Roku control protocol.'], 422);
    else reply(['ok' => false, 'message' => 'Unsupported Roku command: ' . $command], 422);
    if (($status ?? 0) < 200 || ($status ?? 0) >= 300) reply(['ok' => false, 'message' => 'Roku did not confirm that command. Status: ' . ($status ?? 'unknown')], 502);
    reply(['ok' => true, 'message' => 'Roku command sent.']);
} catch (Throwable $error) {
    reply(['ok' => false, 'message' => 'Roku remote failed safely.'], 500);
}
