using NAudio.Wave;

namespace DoweLanCaster.Services;

public sealed class PcAudioMonitorService : IAsyncDisposable
{
    private readonly object _sync = new();
    private MediaFoundationReader? _reader;
    private WaveOutEvent? _output;

    public bool IsPlaying => _output?.PlaybackState == PlaybackState.Playing;

    public async Task StartAsync(
        string streamUrl,
        CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(streamUrl))
            throw new ArgumentException("A stream URL is required.", nameof(streamUrl));

        await StopAsync();

        await Task.Run(() =>
        {
            token.ThrowIfCancellationRequested();

            var reader = new MediaFoundationReader(streamUrl);
            var output = new WaveOutEvent
            {
                DesiredLatency = 150
            };

            try
            {
                output.Init(reader);
                output.Play();

                lock (_sync)
                {
                    _reader = reader;
                    _output = output;
                }
            }
            catch
            {
                output.Dispose();
                reader.Dispose();
                throw;
            }
        }, token);
    }

    public Task StopAsync() => Task.Run(() =>
    {
        lock (_sync)
        {
            _output?.Stop();
            _output?.Dispose();
            _output = null;

            _reader?.Dispose();
            _reader = null;
        }
    });

    public async ValueTask DisposeAsync() => await StopAsync();
}
