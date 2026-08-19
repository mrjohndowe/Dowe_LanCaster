using System.Diagnostics;
using System.IO;
using System.Text;
using DoweLanCaster.Models;

namespace DoweLanCaster.Services;

public sealed class UrlStreamCaptureService : IAsyncDisposable
{
    private Process? _process;
    private readonly Queue<string> _recentLog = new();

    public string OutputDirectory { get; private set; } = "";
    public bool IsRunning => _process is { HasExited: false };

    public event Action<string>? LogLine;

    public async Task StartAsync(
        string ffmpegPath,
        ExtractedMedia media,
        string encoder,
        int bitrateKbps,
        CancellationToken token = default)
    {
        await StopAsync();

        OutputDirectory = Path.Combine(
            Path.GetTempPath(),
            "DoweLanCaster",
            "url-" + Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(OutputDirectory);

        string playlist = Path.Combine(OutputDirectory, "index.m3u8");
        string segments = Path.Combine(OutputDirectory, "seg-%06d.ts");

        string rate = $"{bitrateKbps}k";
        string buffer = $"{bitrateKbps * 2}k";

        string videoEncoding = encoder switch
        {
            "h264_nvenc" =>
                $"-c:v h264_nvenc -preset p3 -tune ll -rc cbr -profile:v high -level:v 4.1 " +
                $"-b:v {rate} -maxrate {rate} -bufsize {buffer}",

            "h264_amf" =>
                $"-c:v h264_amf -usage lowlatency -quality speed -profile:v high -level:v 4.1 " +
                $"-b:v {rate} -maxrate {rate} -bufsize {buffer}",

            "h264_qsv" =>
                $"-c:v h264_qsv -preset veryfast -profile:v high -level:v 4.1 " +
                $"-b:v {rate} -maxrate {rate} -bufsize {buffer}",

            _ =>
                $"-c:v libx264 -preset veryfast -profile:v high -level:v 4.1 " +
                $"-b:v {rate} -maxrate {rate} -bufsize {buffer}"
        };

        var psi = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = OutputDirectory
        };

        psi.ArgumentList.Add("-hide_banner");
        psi.ArgumentList.Add("-y");

        if (media.HttpHeaders.Count > 0)
        {
            var headerBuilder = new StringBuilder();

            foreach (var pair in media.HttpHeaders)
            {
                if (string.Equals(pair.Key, "Accept-Encoding", StringComparison.OrdinalIgnoreCase))
                    continue;

                headerBuilder.Append(pair.Key)
                    .Append(": ")
                    .Append(pair.Value)
                    .Append("\r\n");
            }

            if (headerBuilder.Length > 0)
            {
                psi.ArgumentList.Add("-headers");
                psi.ArgumentList.Add(headerBuilder.ToString());
            }
        }

        psi.ArgumentList.Add("-reconnect");
        psi.ArgumentList.Add("1");
        psi.ArgumentList.Add("-reconnect_streamed");
        psi.ArgumentList.Add("1");
        psi.ArgumentList.Add("-reconnect_delay_max");
        psi.ArgumentList.Add("5");

        psi.ArgumentList.Add("-i");
        psi.ArgumentList.Add(media.MediaUrl);

        foreach (var arg in videoEncoding.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            psi.ArgumentList.Add(arg);

        psi.ArgumentList.Add("-c:a");
        psi.ArgumentList.Add("aac");
        psi.ArgumentList.Add("-b:a");
        psi.ArgumentList.Add("160k");
        psi.ArgumentList.Add("-ac");
        psi.ArgumentList.Add("2");
        psi.ArgumentList.Add("-ar");
        psi.ArgumentList.Add("48000");
        psi.ArgumentList.Add("-pix_fmt");
        psi.ArgumentList.Add("yuv420p");
        psi.ArgumentList.Add("-force_key_frames");
        psi.ArgumentList.Add("expr:gte(t,n_forced*2)");
        psi.ArgumentList.Add("-f");
        psi.ArgumentList.Add("hls");
        psi.ArgumentList.Add("-hls_segment_type");
        psi.ArgumentList.Add("mpegts");
        psi.ArgumentList.Add("-hls_time");
        psi.ArgumentList.Add("2");
        psi.ArgumentList.Add("-hls_list_size");
        psi.ArgumentList.Add(media.IsLive ? "10" : "0");
        psi.ArgumentList.Add("-hls_flags");
        psi.ArgumentList.Add(
            media.IsLive
                ? "delete_segments+omit_endlist+independent_segments"
                : "independent_segments");
        psi.ArgumentList.Add("-hls_segment_filename");
        psi.ArgumentList.Add(segments);
        psi.ArgumentList.Add(playlist);

        var process = new Process
        {
            StartInfo = psi,
            EnableRaisingEvents = true
        };

        process.ErrorDataReceived += (_, e) =>
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

        if (!process.Start())
            throw new InvalidOperationException("FFmpeg could not be started.");

        process.BeginErrorReadLine();
        _process = process;

        for (int i = 0; i < 150; i++)
        {
            token.ThrowIfCancellationRequested();

            if (process.HasExited)
            {
                string details;
                lock (_recentLog)
                    details = string.Join(Environment.NewLine, _recentLog);

                throw new InvalidOperationException(
                    $"FFmpeg exited with code {process.ExitCode}.{Environment.NewLine}{details}");
            }

            if (File.Exists(playlist))
            {
                string text = await TryReadAsync(playlist, token);
                if (text.Contains("#EXTINF:", StringComparison.Ordinal))
                    return;
            }

            await Task.Delay(100, token);
        }

        throw new TimeoutException(
            "The video was extracted, but FFmpeg did not create a playable HLS stream.");
    }

    private static async Task<string> TryReadAsync(string path, CancellationToken token)
    {
        try
        {
            await using var stream = new FileStream(
                path, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);

            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync(token);
        }
        catch (IOException)
        {
            return "";
        }
    }

    public async Task StopAsync()
    {
        var process = _process;
        _process = null;

        if (process is not null)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    await process.WaitForExitAsync();
                }
            }
            catch { }
            finally { process.Dispose(); }
        }

        if (!string.IsNullOrWhiteSpace(OutputDirectory) &&
            Directory.Exists(OutputDirectory))
        {
            try { Directory.Delete(OutputDirectory, recursive: true); }
            catch { }
        }

        OutputDirectory = "";

        lock (_recentLog)
            _recentLog.Clear();
    }

    public async ValueTask DisposeAsync() => await StopAsync();
}
