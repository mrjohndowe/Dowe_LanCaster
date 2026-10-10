using System.Diagnostics;
using DoweLanCaster.Models;

namespace DoweLanCaster.Services;

public sealed class IptvPlaybackService
{
    private Process? _vlcProcess;
    private CancellationTokenSource? _playbackCancellation;
    private int _currentRetryCount;
    private IptvChannel? _currentChannel;
    private TaskCompletionSource<bool>? _vlcStartedCompletion;

    public event EventHandler? PlaybackStarted;
    public event EventHandler? PlaybackStopped;
    public event EventHandler<string>? PlaybackError;
    public event EventHandler<IptvChannel>? ChannelChanged;

    public bool IsPlaying => _vlcProcess != null && !_vlcProcess.HasExited;
    public IptvChannel? CurrentChannel => _currentChannel;

    public async Task PlayChannelAsync(IptvChannel channel, string vlcPath, int maxRetries, CancellationToken cancellationToken = default)
    {
        _playbackCancellation?.Cancel();
        _playbackCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        StopVlc();

        _currentChannel = channel;
        _currentRetryCount = 0;

        ChannelChanged?.Invoke(this, channel);

        await PlayWithRetryAsync(channel, vlcPath, maxRetries, _playbackCancellation.Token);
    }

    private async Task PlayWithRetryAsync(IptvChannel channel, string vlcPath, int maxRetries, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _currentRetryCount <= maxRetries)
        {
            try
            {
                _vlcStartedCompletion = new TaskCompletionSource<bool>();
                LaunchVlc(channel.Url, vlcPath);

                var started = await Task.WhenAny(
                    _vlcStartedCompletion.Task,
                    Task.Delay(5000, cancellationToken));

                if (started == _vlcStartedCompletion.Task && _vlcStartedCompletion.Task.Result)
                {
                    PlaybackStarted?.Invoke(this, EventArgs.Empty);
                    _currentRetryCount = 0;
                    return;
                }

                _currentRetryCount++;
                StopVlc();

                if (_currentRetryCount > maxRetries)
                {
                    PlaybackError?.Invoke(this, $"Failed to start VLC after {maxRetries} retries");
                    return;
                }

                await Task.Delay(2000, cancellationToken);
            }
            catch (Exception ex)
            {
                _currentRetryCount++;
                StopVlc();

                if (_currentRetryCount > maxRetries)
                {
                    PlaybackError?.Invoke(this, $"Failed to play channel after {maxRetries} retries: {ex.Message}");
                    return;
                }

                await Task.Delay(2000, cancellationToken);
            }
        }
    }

    private void LaunchVlc(string url, string vlcPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = vlcPath,
            Arguments = $"--no-video-title-show \"{url}\"",
            UseShellExecute = false,
            CreateNoWindow = false
        };

        _vlcProcess = Process.Start(startInfo);
        if (_vlcProcess == null)
        {
            throw new InvalidOperationException("Failed to start VLC process.");
        }

        _vlcProcess.EnableRaisingEvents = true;
        _vlcProcess.Exited += (s, e) =>
        {
            _vlcStartedCompletion?.TrySetResult(false);

            if (_vlcProcess?.ExitCode != 0)
            {
                PlaybackError?.Invoke(this, $"VLC exited with code {_vlcProcess.ExitCode}");
            }
            PlaybackStopped?.Invoke(this, EventArgs.Empty);
        };

        _vlcStartedCompletion?.TrySetResult(true);
    }

    public void Stop()
    {
        _playbackCancellation?.Cancel();
        StopVlc();
        _currentChannel = null;
        PlaybackStopped?.Invoke(this, EventArgs.Empty);
    }

    private void StopVlc()
    {
        if (_vlcProcess != null && !_vlcProcess.HasExited)
        {
            try
            {
                _vlcProcess.Kill(entireProcessTree: true);
                _vlcProcess.WaitForExit(1000);
            }
            catch
            {
            }
        }
        _vlcProcess = null;
    }

    public void Dispose()
    {
        Stop();
        _playbackCancellation?.Dispose();
    }
}
