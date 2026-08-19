using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using DoweLanCaster.Models;
using NAudio.Wave;

namespace DoweLanCaster.Services;

public sealed class LiveCaptureService : IAsyncDisposable
{
    private Process? _process;
    private readonly Queue<string> _recentLog = new();

    private WasapiLoopbackCapture? _loopbackCapture;
    private NamedPipeServerStream? _audioPipe;
    private readonly object _audioWriteLock = new();

    public string OutputDirectory { get; private set; } = "";
    public bool IsRunning => _process is { HasExited: false };

    public event Action<string>? LogLine;

    public async Task StartAsync(
        string ffmpegPath,
        CaptureSource source,
        string encoder,
        string? audioDevice,
        int fps,
        int bitrateKbps,
        CancellationToken token = default)
    {
        await StopAsync();

        OutputDirectory = Path.Combine(
            Path.GetTempPath(),
            "DoweLanCaster",
            "live-" + Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(OutputDirectory);

        string playlist = Path.Combine(OutputDirectory, "index.m3u8");
        string segments = Path.Combine(OutputDirectory, "seg-%06d.ts");

        string videoInput = source.Type switch
        {
            CaptureSourceType.Desktop =>
                $"-f gdigrab -framerate {fps} -draw_mouse 1 -i desktop",

            CaptureSourceType.Monitor =>
                $"-f gdigrab -framerate {fps} -draw_mouse 1 " +
                $"-offset_x {source.Left} -offset_y {source.Top} " +
                $"-video_size {source.Width}x{source.Height} -i desktop",

            CaptureSourceType.Window =>
                $"-f gdigrab -framerate {fps} -draw_mouse 1 " +
                $"-i title=\"{Escape(source.WindowTitle ?? "")}\"",

            _ => throw new NotSupportedException("Unsupported capture source.")
        };

        bool useLoopback =
            string.Equals(
                audioDevice,
                "__SYSTEM_LOOPBACK__",
                StringComparison.Ordinal);

        string audioInput = "";
        string maps = "-map 0:v:0";
        string audioEncoding = "-an";

        if (useLoopback)
        {
            _loopbackCapture = new WasapiLoopbackCapture();

            var format = _loopbackCapture.WaveFormat;
            var pipeName = "DoweLanCasterAudio-" + Guid.NewGuid().ToString("N");
            var pipePath = $@"\\.\pipe\{pipeName}";

            _audioPipe = new NamedPipeServerStream(
                pipeName,
                PipeDirection.Out,
                1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);

            string ffmpegPcmFormat = GetFfmpegPcmFormat(format);

            audioInput =
                $" -f {ffmpegPcmFormat}" +
                $" -ar {format.SampleRate}" +
                $" -ac {format.Channels}" +
                $" -i \"{pipePath}\"";

            maps = "-map 0:v:0 -map 1:a:0";
            audioEncoding = "-c:a aac -b:a 160k -ac 2 -ar 48000";
        }
        else if (!string.IsNullOrWhiteSpace(audioDevice))
        {
            audioInput =
                $" -f dshow -i audio=\"{Escape(audioDevice)}\"";

            maps = "-map 0:v:0 -map 1:a:0";
            audioEncoding = "-c:a aac -b:a 160k -ac 2 -ar 48000";
        }

        string rate = $"{bitrateKbps}k";
        string buffer = $"{bitrateKbps * 2}k";

        string videoEncoding = encoder switch
        {
            "h264_nvenc" =>
                $"-c:v h264_nvenc -preset p3 -tune ll -rc cbr " +
                $"-profile:v high -level:v 4.1 " +
                $"-b:v {rate} -maxrate {rate} -bufsize {buffer}",

            "h264_amf" =>
                $"-c:v h264_amf -usage lowlatency -quality speed " +
                $"-profile:v high -level:v 4.1 " +
                $"-b:v {rate} -maxrate {rate} -bufsize {buffer}",

            "h264_qsv" =>
                $"-c:v h264_qsv -preset veryfast " +
                $"-profile:v high -level:v 4.1 " +
                $"-b:v {rate} -maxrate {rate} -bufsize {buffer}",

            _ =>
                $"-c:v libx264 -preset veryfast -tune zerolatency " +
                $"-profile:v high -level:v 4.1 " +
                $"-b:v {rate} -maxrate {rate} -bufsize {buffer}"
        };

        string args =
            $"-hide_banner -y {videoInput}{audioInput} {maps} " +
            $"{videoEncoding} {audioEncoding} " +
            $"-pix_fmt yuv420p " +
            $"-g {fps * 2} -keyint_min {fps * 2} -sc_threshold 0 " +
            $"-f hls " +
            $"-hls_segment_type mpegts " +
            $"-hls_time 2 " +
            $"-hls_list_size 10 " +
            $"-hls_delete_threshold 5 " +
            $"-hls_flags delete_segments+omit_endlist+independent_segments " +
            $"-hls_segment_filename \"{segments}\" " +
            $"\"{playlist}\"";

        var psi = new ProcessStartInfo(ffmpegPath, args)
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = OutputDirectory
        };

        var p = new Process
        {
            StartInfo = psi,
            EnableRaisingEvents = true
        };

        p.ErrorDataReceived += (_, e) =>
        {
            if (string.IsNullOrWhiteSpace(e.Data))
                return;

            lock (_recentLog)
            {
                _recentLog.Enqueue(e.Data);

                while (_recentLog.Count > 20)
                    _recentLog.Dequeue();
            }

            LogLine?.Invoke(e.Data);
        };

        if (!p.Start())
            throw new InvalidOperationException("FFmpeg could not be started.");

        p.BeginErrorReadLine();
        _process = p;

        if (useLoopback)
            await StartLoopbackCaptureAsync(token);

        for (int i = 0; i < 120; i++)
        {
            token.ThrowIfCancellationRequested();

            if (p.HasExited)
            {
                string details;

                lock (_recentLog)
                    details = string.Join(Environment.NewLine, _recentLog);

                throw new InvalidOperationException(
                    $"FFmpeg exited early with code {p.ExitCode}.{Environment.NewLine}{details}");
            }

            if (File.Exists(playlist))
            {
                var text =
                    await ReadPlaylistWhenReadyAsync(
                        playlist,
                        token);

                if (text.Contains("#EXTINF:", StringComparison.Ordinal))
                    return;
            }

            await Task.Delay(100, token);
        }

        throw new TimeoutException(
            "FFmpeg started but did not create a playable HLS playlist.");
    }

    private async Task StartLoopbackCaptureAsync(
        CancellationToken token)
    {
        if (_audioPipe is null ||
            _loopbackCapture is null)
        {
            throw new InvalidOperationException(
                "System audio loopback was not initialized.");
        }

        using var timeout =
            CancellationTokenSource.CreateLinkedTokenSource(token);

        timeout.CancelAfter(TimeSpan.FromSeconds(8));

        await _audioPipe.WaitForConnectionAsync(timeout.Token);

        _loopbackCapture.DataAvailable += OnLoopbackDataAvailable;
        _loopbackCapture.StartRecording();

        LogLine?.Invoke(
            $"System audio loopback started: " +
            $"{_loopbackCapture.WaveFormat.SampleRate} Hz, " +
            $"{_loopbackCapture.WaveFormat.Channels} channel(s).");
    }

    private void OnLoopbackDataAvailable(
        object? sender,
        WaveInEventArgs e)
    {
        if (_audioPipe is null ||
            !_audioPipe.IsConnected ||
            e.BytesRecorded <= 0)
        {
            return;
        }

        try
        {
            lock (_audioWriteLock)
            {
                _audioPipe.Write(
                    e.Buffer,
                    0,
                    e.BytesRecorded);

                _audioPipe.Flush();
            }
        }
        catch
        {
            // Stream shutdown can race with the capture callback.
        }
    }

    private static string GetFfmpegPcmFormat(
        WaveFormat format)
    {
        return format.BitsPerSample switch
        {
            16 => "s16le",
            24 => "s24le",
            32 => "f32le",
            _ => "f32le"
        };
    }

    private static async Task<string> ReadPlaylistWhenReadyAsync(
        string playlist,
        CancellationToken token)
    {
        for (int i = 0; i < 10; i++)
        {
            try
            {
                using var stream = new FileStream(
                    playlist,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete);

                using var reader = new StreamReader(stream);
                return await reader.ReadToEndAsync(token);
            }
            catch (IOException)
            {
                await Task.Delay(50, token);
            }
        }

        return "";
    }

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    public async Task StopAsync()
    {
        if (_loopbackCapture is not null)
        {
            try
            {
                _loopbackCapture.DataAvailable -= OnLoopbackDataAvailable;
                _loopbackCapture.StopRecording();
            }
            catch
            {
            }
            finally
            {
                _loopbackCapture.Dispose();
                _loopbackCapture = null;
            }
        }

        if (_audioPipe is not null)
        {
            try
            {
                _audioPipe.Dispose();
            }
            catch
            {
            }
            finally
            {
                _audioPipe = null;
            }
        }

        var p = _process;
        _process = null;

        if (p is not null)
        {
            try
            {
                if (!p.HasExited)
                {
                    p.Kill(entireProcessTree: true);
                    await p.WaitForExitAsync();
                }
            }
            catch
            {
            }
            finally
            {
                p.Dispose();
            }
        }

        if (!string.IsNullOrWhiteSpace(OutputDirectory) &&
            Directory.Exists(OutputDirectory))
        {
            try
            {
                Directory.Delete(
                    OutputDirectory,
                    recursive: true);
            }
            catch
            {
            }
        }

        OutputDirectory = "";

        lock (_recentLog)
            _recentLog.Clear();
    }

    public async ValueTask DisposeAsync() =>
        await StopAsync();
}
