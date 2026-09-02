using System.IO;
using System.Speech.Recognition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using DoweLanCaster.Models;
using DoweLanCaster.Services;
using Microsoft.Win32;

namespace DoweLanCaster;

public partial class MainWindow : Window
{
    private readonly RokuDiscoveryService _discoveryService = new();
    private readonly MediaStreamingServer _mediaServer = new();
    private readonly LiveStreamingServer _liveServer = new();
    private readonly LiveCaptureService _liveCapture = new();
    private readonly CaptureSourceService _captureSources = new();
    private readonly EncoderDetectionService _encoderDetection = new();
    private readonly LiveStreamingServer _urlServer = new();
    private readonly UrlStreamCaptureService _urlCapture = new();
    private readonly MediaLinkExtractorService _linkExtractor = new();
    private readonly AudioBackendService _audioBackendService = new();
    private readonly PcAudioMonitorService _pcAudioMonitor = new();
    private readonly SettingsService _settingsService = new();
    private readonly UpdateService _updateService = new();
    private readonly DiagnosticState _diagnostics = new();
    private readonly FolderPlaylistService _folderPlaylistService = new();
    private readonly LocalFileHlsTranscoder _folderTranscoder = new();
    private readonly LiveStreamingServer _folderServer = new();
    private readonly DispatcherTimer _folderPollTimer = new()
    {
        Interval = TimeSpan.FromSeconds(2)
    };

    private RokuClient? _rokuClient;
    private string? _selectedFile;
    private string? _ffmpegPath;
    private string? _ytDlpPath;
    private ExtractedMedia? _extractedMedia;
    private AppSettings _settings = new();
    private List<FolderMediaItem> _folderItems = new();
    private int _folderIndex = -1;
    private bool _folderSawPlaying;
    private bool _folderAdvanceInProgress;
    private bool _folderReceiverLaunched;
    private SpeechRecognitionEngine? _voiceRecognizer;
    private bool _voiceListening;
    private string? _pcAudioMonitorUrl;
    private RemoteWindow? _remoteWindow;

    private static readonly IReadOnlyDictionary<string, string> VoiceCommandMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["home"] = "Home",
            ["back"] = "Back",
            ["up"] = "Up",
            ["down"] = "Down",
            ["left"] = "Left",
            ["right"] = "Right",
            ["ok"] = "Select",
            ["okay"] = "Select",
            ["select"] = "Select",
            ["play"] = "Play",
            ["pause"] = "Play",
            ["play pause"] = "Play",
            ["rewind"] = "Rev",
            ["fast forward"] = "Fwd",
            ["forward"] = "Fwd",
            ["replay"] = "InstantReplay",
            ["volume up"] = "VolumeUp",
            ["volume down"] = "VolumeDown",
            ["mute"] = "VolumeMute",
            ["power"] = "Power"
        };

    public MainWindow()
    {
        InitializeComponent();

        CurrentVersionText.Text =
            $"v{_updateService.CurrentVersion}";

        _settings = _settingsService.Load();

        _folderPollTimer.Tick += FolderPollTimer_Tick;

        Loaded += async (_, _) =>
        {
            LoadSavedSettings();
            RefreshCaptureSources();
            await InitializeFFmpegAsync();
            InitializeYtDlp();
            await RefreshAudioSourcesAsync();

            if (!string.IsNullOrWhiteSpace(_settings.LastFolderPath) &&
                Directory.Exists(_settings.LastFolderPath))
            {
                ScanFolder(_settings.LastFolderPath);
            }

            UpdateDiagnostics(
                hls: "Stopped",
                message: "Dowe LanCaster ready.");
        };

        _liveCapture.LogLine += line =>
        {
            Dispatcher.Invoke(() =>
            {
                if (line.Contains("error", StringComparison.OrdinalIgnoreCase))
                    LiveStatusText.Text = line;

                _diagnostics.LastMessage = line;
                UpdateDiagnostics();
            });
        };

        _linkExtractor.LogLine += line =>
        {
            Dispatcher.Invoke(() =>
            {
                LinkStatusText.Text = line;
                _diagnostics.LastMessage = line;
                UpdateDiagnostics();
            });
        };

        _urlCapture.LogLine += line =>
        {
            Dispatcher.Invoke(() =>
            {
                if (line.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("HTTP error", StringComparison.OrdinalIgnoreCase))
                {
                    LinkStatusText.Text = line;
                }

                _diagnostics.LastMessage = line;
                UpdateDiagnostics();
            });
        };
    }

    private async Task InitializeFFmpegAsync()
    {
        _ffmpegPath = FFmpegLocator.FindFFmpeg();

        if (string.IsNullOrWhiteSpace(_ffmpegPath))
        {
            SetFfmpegUnavailableOptions();
            LiveStatusText.Text =
                "FFmpeg not found. Run SETUP-DEPENDENCIES.cmd or place ffmpeg.exe in tools\\ffmpeg.";
            UpdateDiagnostics(message: "FFmpeg not found.");
            return;
        }

        try
        {
            var encoders =
                await _encoderDetection.DetectAsync(_ffmpegPath);

            EncoderComboBox.ItemsSource = encoders;
            LinkEncoderComboBox.ItemsSource = encoders;
            FolderEncoderComboBox.ItemsSource = encoders;

            var preferred = encoders.Contains(
                _settings.PreferredEncoder,
                StringComparer.OrdinalIgnoreCase)
                ? _settings.PreferredEncoder
                : encoders.FirstOrDefault();

            EncoderComboBox.SelectedItem = preferred;
            LinkEncoderComboBox.SelectedItem = preferred;
            FolderEncoderComboBox.SelectedItem = preferred;
            EncoderComboBox.IsEnabled = true;
            LinkEncoderComboBox.IsEnabled = true;
            FolderEncoderComboBox.IsEnabled = true;

            LiveStatusText.Text =
                $"FFmpeg ready: {_ffmpegPath}";

            UpdateDiagnostics(message: "FFmpeg initialized.");
        }
        catch (Exception ex)
        {
            LiveStatusText.Text =
                $"FFmpeg detection failed: {ex.Message}";
            UpdateDiagnostics(message: ex.Message);
        }
    }

    private void InitializeYtDlp()
    {
        _ytDlpPath = YtDlpLocator.Find();

        AnalyzeLinkButton.IsEnabled = true;

        if (string.IsNullOrWhiteSpace(_ytDlpPath))
        {
            LinkStatusText.Text =
                "yt-dlp is not installed. Direct media URLs still work; webpage extraction needs tools\\yt-dlp\\yt-dlp.exe.";
            UpdateDiagnostics(message: "yt-dlp not found; direct media mode available.");
            return;
        }

        LinkStatusText.Text =
            $"yt-dlp ready: {_ytDlpPath}";
        UpdateDiagnostics(message: "yt-dlp initialized.");
    }

    private async void AnalyzeLinkButton_Click(object sender, RoutedEventArgs e)
    {
        var url = LinkUrlTextBox.Text.Trim();

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp &&
             uri.Scheme != Uri.UriSchemeHttps))
        {
            LinkStatusText.Text =
                "Enter a valid HTTP or HTTPS video URL.";
            return;
        }

        AnalyzeLinkButton.IsEnabled = false;
        StreamLinkButton.IsEnabled = false;
        _extractedMedia = null;

        try
        {
            LinkTitleText.Text = "-";
            LinkProtocolText.Text = "-";
            LinkTypeText.Text = "-";
            LinkStatusText.Text = "Analyzing link...";

            if (DirectMediaDetector.IsDirectMediaUrl(
                    url,
                    out var extension))
            {
                var path = uri.AbsolutePath.TrimEnd('/');
                var isTeraBoxStream =
                    DirectMediaDetector.IsTeraBoxStreamingUrl(url);

                _extractedMedia = new ExtractedMedia
                {
                    PageUrl = url,
                    MediaUrl = url,
                    Title = isTeraBoxStream
                        ? "TeraBox video"
                        : Path.GetFileNameWithoutExtension(path),
                    Protocol =
                        extension.Equals(
                            ".m3u8",
                            StringComparison.OrdinalIgnoreCase)
                            ? "HLS"
                            : "Direct HTTP",
                    Extension = extension.TrimStart('.'),
                    IsLive =
                        extension.Equals(
                            ".m3u8",
                            StringComparison.OrdinalIgnoreCase),
                    HttpHeaders = isTeraBoxStream
                        ? new Dictionary<string, string>(
                            StringComparer.OrdinalIgnoreCase)
                        {
                            ["Referer"] = $"{uri.Scheme}://{uri.Host}/",
                            ["User-Agent"] =
                                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
                                "AppleWebKit/537.36 (KHTML, like Gecko) " +
                                "Chrome/124.0.0.0 Safari/537.36"
                        }
                        : new Dictionary<string, string>(
                            StringComparer.OrdinalIgnoreCase)
                };

                LinkTitleText.Text =
                    string.IsNullOrWhiteSpace(_extractedMedia.Title)
                        ? "Direct video"
                        : _extractedMedia.Title;

                LinkProtocolText.Text =
                    _extractedMedia.Protocol;

                LinkTypeText.Text =
                    _extractedMedia.IsLive
                        ? "HLS media"
                        : "Direct media";

                LinkStatusText.Text = isTeraBoxStream
                    ? "TeraBox HLS stream detected. Ready to stream to Roku."
                    : "Direct media detected. Ready to stream to Roku.";

                StreamLinkButton.IsEnabled = true;
                UpdateDiagnostics(
                    message: "Direct media URL detected; yt-dlp bypassed.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_ytDlpPath) ||
                !File.Exists(_ytDlpPath))
            {
                _ytDlpPath = YtDlpLocator.Find();
            }

            LinkStatusText.Text =
                string.IsNullOrWhiteSpace(_ytDlpPath)
                    ? "Webpage detected. Scanning for embedded media..."
                    : "Webpage detected. Extracting media...";

            var media =
                await _linkExtractor.ExtractAsync(
                    _ytDlpPath,
                    url);

            _extractedMedia = media;

            LinkTitleText.Text =
                string.IsNullOrWhiteSpace(media.Title)
                    ? "(untitled video)"
                    : media.Title;

            LinkProtocolText.Text =
                string.IsNullOrWhiteSpace(media.Protocol)
                    ? "HTTP media"
                    : media.Protocol;

            LinkTypeText.Text =
                media.IsLive
                    ? "Live source"
                    : "On-demand source";

            LinkStatusText.Text =
                "Video found. Ready to stream to Roku.";

            StreamLinkButton.IsEnabled = true;
            UpdateDiagnostics(
                message: "yt-dlp extraction succeeded.");
        }
        catch (Exception ex)
        {
            LinkStatusText.Text =
                $"Could not analyze video: {ex.Message}";
            UpdateDiagnostics(message: ex.Message);
        }
        finally
        {
            AnalyzeLinkButton.IsEnabled = true;
        }
    }

    private async void StreamLinkButton_Click(object sender, RoutedEventArgs e)
    {
        if (_rokuClient is null)
        {
            LinkStatusText.Text =
                "Select a Roku device first.";
            return;
        }

        if (_extractedMedia is null)
        {
            LinkStatusText.Text =
                "Analyze the link first.";
            return;
        }

        if (string.IsNullOrWhiteSpace(_ffmpegPath))
        {
            LinkStatusText.Text =
                "FFmpeg was not found. Run SETUP-DEPENDENCIES.cmd.";
            return;
        }

        StreamLinkButton.IsEnabled = false;

        try
        {
            await StopLinkInternalAsync(
                sendHome: false);

            string friendlyEncoder =
                LinkEncoderComboBox.SelectedItem?.ToString()
                ?? "CPU (libx264)";

            string encoder =
                EncoderDetectionService.ToFFmpegEncoder(
                    friendlyEncoder);

            string bitrateText =
                ((ComboBoxItem)LinkBitrateComboBox.SelectedItem)
                .Content.ToString()!;

            int bitrate =
                int.Parse(
                    bitrateText.Split(' ')[0]);

            LinkStatusText.Text =
                $"Preparing {_extractedMedia.Title} using {friendlyEncoder}...";

            await _urlCapture.StartAsync(
                _ffmpegPath,
                _extractedMedia,
                encoder,
                bitrateKbps: bitrate);

            await _urlServer.StartAsync(
                _urlCapture.OutputDirectory,
                port: 8767);

            var ip =
                NetworkHelper.GetBestLocalIPv4ForRemote(
                    _rokuClient.Device.IpAddress)
                ?? throw new InvalidOperationException(
                    "Could not determine the PC LAN IP.");

            var streamUrl =
                $"http://{ip}:{_urlServer.Port}/live/index.m3u8";

            LinkStreamUrlTextBox.Text =
                streamUrl;
            SetPcAudioMonitorSource(streamUrl);

            LinkStatusText.Text =
                "Launching Dowe LanCaster on the Roku...";

            await _rokuClient
                .LaunchDoweLanCasterLiveAsync(
                    streamUrl);

            StopLinkButton.IsEnabled = true;
            StatusText.Text = "Link casting active.";

            LinkStatusText.Text =
                $"Streaming {_extractedMedia.Title} " +
                $"to {_rokuClient.Device.Name}.";

            SaveCurrentSettings();

            UpdateDiagnostics(
                hls: "Link Cast running",
                streamUrl: streamUrl,
                message: LinkStatusText.Text);
        }
        catch (Exception ex)
        {
            await StopLinkInternalAsync(
                sendHome: false);

            LinkStatusText.Text =
                $"Link stream failed: {ex.Message}";

            StatusText.Text =
                "Link cast failed.";

            UpdateDiagnostics(
                hls: "Link Cast failed",
                message: ex.Message);
        }
        finally
        {
            StreamLinkButton.IsEnabled =
                _extractedMedia is not null;
        }
    }

    private async void StopLinkButton_Click(object sender, RoutedEventArgs e)
    {
        await StopLinkInternalAsync(sendHome: true);
    }

    private async Task StopLinkInternalAsync(bool sendHome)
    {
        StopLinkButton.IsEnabled = false;

        if (sendHome && _rokuClient is not null)
        {
            try
            {
                await _rokuClient.SendKeyAsync("Home");
            }
            catch
            {
            }
        }

        await _urlServer.StopAsync();
        await _urlCapture.StopAsync();
        LinkStreamUrlTextBox.Clear();
        await StopPcAudioMonitorAsync();

        if (sendHome)
        {
            LinkStatusText.Text = "Link streaming stopped.";
            StatusText.Text = "Ready.";
        }
    }

    private void LoadSavedSettings()
    {
        ManualRokuIpTextBox.Text =
            _settings.LastRokuIp ?? "";

        IncludeAudioCheckBox.IsChecked =
            _settings.IncludeSystemAudio;

        FolderPathTextBox.Text =
            _settings.LastFolderPath ?? "";

        FolderIncludeSubfoldersCheckBox.IsChecked =
            _settings.FolderIncludeSubfolders;

        FolderAutoPlayCheckBox.IsChecked =
            _settings.FolderAutoPlayNext;

        DarkModeCheckBox.IsChecked =
            _settings.UseDarkMode;

        ThemeService.ApplyTheme(
            _settings.UseDarkMode);

        SelectComboItemByContent(
            FolderRepeatComboBox,
            _settings.FolderRepeatMode);

        SelectComboItemByContent(
            FpsComboBox,
            _settings.PreferredFps.ToString());

        SelectComboItemByContent(
            BitrateComboBox,
            $"{_settings.PreferredBitrateKbps} kbps");

        SelectComboItemByContent(
            LinkBitrateComboBox,
            $"{_settings.PreferredBitrateKbps} kbps");
    }

    private static void SelectComboItemByContent(
        System.Windows.Controls.ComboBox combo,
        string desired)
    {
        foreach (var item in combo.Items)
        {
            if (item is ComboBoxItem comboItem &&
                string.Equals(
                    comboItem.Content?.ToString(),
                    desired,
                    StringComparison.OrdinalIgnoreCase))
            {
                combo.SelectedItem = comboItem;
                return;
            }
        }
    }

    private async Task RefreshAudioSourcesAsync()
    {
        if (string.IsNullOrWhiteSpace(_ffmpegPath))
        {
            AudioDeviceComboBox.ItemsSource =
                new[]
                {
                    new AudioSourceOption
                    {
                        Kind = AudioSourceKind.None,
                        Name = "No Audio (FFmpeg unavailable)"
                    }
                };
            AudioDeviceComboBox.SelectedIndex = 0;
            AudioDeviceComboBox.IsEnabled = false;
            IncludeAudioCheckBox.IsChecked = false;
            IncludeAudioCheckBox.IsEnabled = false;
            LiveStatusText.Text =
                "FFmpeg is required for audio capture.";
            return;
        }

        try
        {
            LiveStatusText.Text =
                "Detecting Windows system audio and input devices...";

            var sources =
                await _audioBackendService.GetAudioSourcesAsync(
                    _ffmpegPath);

            AudioDeviceComboBox.ItemsSource = sources;

            var preferred =
                sources.FirstOrDefault(x =>
                    string.Equals(
                        x.ToString(),
                        _settings.PreferredAudioSource,
                        StringComparison.OrdinalIgnoreCase));

            var systemAudio =
                sources.FirstOrDefault(x =>
                    x.Kind == AudioSourceKind.SystemLoopback);

            AudioDeviceComboBox.SelectedItem =
                preferred
                ?? systemAudio
                ?? sources.FirstOrDefault();
            AudioDeviceComboBox.IsEnabled = true;
            IncludeAudioCheckBox.IsEnabled = true;

            LiveStatusText.Text =
                $"Found {sources.Count} audio option(s).";

            UpdateDiagnostics(
                message: "Audio sources refreshed.");
        }
        catch (Exception ex)
        {
            LiveStatusText.Text =
                $"Audio detection failed: {ex.Message}";
            UpdateDiagnostics(message: ex.Message);
        }
    }

    private void SetFfmpegUnavailableOptions()
    {
        var unavailable = new[] { "FFmpeg unavailable" };

        EncoderComboBox.ItemsSource = unavailable;
        LinkEncoderComboBox.ItemsSource = unavailable;
        FolderEncoderComboBox.ItemsSource = unavailable;
        EncoderComboBox.SelectedIndex = 0;
        LinkEncoderComboBox.SelectedIndex = 0;
        FolderEncoderComboBox.SelectedIndex = 0;
        EncoderComboBox.IsEnabled = false;
        LinkEncoderComboBox.IsEnabled = false;
        FolderEncoderComboBox.IsEnabled = false;
    }

    private static string SelectBestEncoder(
        System.Windows.Controls.ComboBox comboBox)
    {
        var encoders = comboBox.Items
            .OfType<string>()
            .ToArray();

        return comboBox.SelectedItem?.ToString()
            ?? encoders.FirstOrDefault()
            ?? "CPU (libx264)";
    }

    private void SaveCurrentSettings()
    {
        if (DeviceComboBox.SelectedItem is RokuDevice device)
            _settings.LastRokuIp = device.IpAddress;
        else if (!string.IsNullOrWhiteSpace(ManualRokuIpTextBox.Text))
            _settings.LastRokuIp = ManualRokuIpTextBox.Text.Trim();

        _settings.PreferredEncoder =
            EncoderComboBox.SelectedItem?.ToString();

        _settings.PreferredAudioSource =
            AudioDeviceComboBox.SelectedItem?.ToString();

        _settings.IncludeSystemAudio =
            IncludeAudioCheckBox.IsChecked == true;

        if (FpsComboBox.SelectedItem is ComboBoxItem fpsItem &&
            int.TryParse(
                fpsItem.Content?.ToString(),
                out var fps))
        {
            _settings.PreferredFps = fps;
        }

        if (BitrateComboBox.SelectedItem is ComboBoxItem bitrateItem)
        {
            var text =
                bitrateItem.Content?.ToString()
                ?? "8000 kbps";

            if (int.TryParse(
                    text.Split(' ')[0],
                    out var bitrate))
            {
                _settings.PreferredBitrateKbps =
                    bitrate;
            }
        }

        _settings.LastFolderPath =
            string.IsNullOrWhiteSpace(FolderPathTextBox.Text)
                ? null
                : FolderPathTextBox.Text.Trim();

        _settings.FolderIncludeSubfolders =
            FolderIncludeSubfoldersCheckBox.IsChecked == true;

        _settings.FolderRepeatMode =
            ((ComboBoxItem?)FolderRepeatComboBox.SelectedItem)
            ?.Content?.ToString()
            ?? "Off";

        _settings.FolderAutoPlayNext =
            FolderAutoPlayCheckBox.IsChecked == true;

        _settings.UseDarkMode =
            DarkModeCheckBox.IsChecked == true;

        _settingsService.Save(_settings);
    }

    private async void AddRokuByIpButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var ip =
            ManualRokuIpTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(ip))
        {
            StatusText.Text =
                "Enter a Roku IP address.";
            return;
        }

        StatusText.Text =
            $"Checking Roku at {ip}...";

        var device =
            await _discoveryService.TryGetDeviceByIpAsync(ip);

        if (device is null)
        {
            StatusText.Text =
                $"No Roku ECP device responded at {ip}:8060.";
            UpdateDiagnostics(
                message: $"Manual Roku connection failed for {ip}.");
            return;
        }

        var current =
            (DeviceComboBox.ItemsSource as IEnumerable<RokuDevice>)
            ?.ToList()
            ?? new List<RokuDevice>();

        if (current.All(x =>
            !x.IpAddress.Equals(
                device.IpAddress,
                StringComparison.OrdinalIgnoreCase)))
        {
            current.Add(device);
        }

        var ordered =
            current.OrderBy(x => x.Name).ToList();

        DeviceComboBox.ItemsSource =
            ordered;

        DeviceComboBox.SelectedItem =
            ordered.First(x =>
                x.IpAddress == device.IpAddress);

        _settings.LastRokuIp =
            device.IpAddress;

        _settingsService.Save(_settings);

        StatusText.Text =
            $"Connected manually to {device.Name}.";

        UpdateDiagnostics(
            message: "Manual Roku connection succeeded.");
    }

    private void UpdateDiagnostics(
        string? hls = null,
        string? streamUrl = null,
        string? message = null)
    {
        if (DeviceComboBox.SelectedItem is RokuDevice device)
        {
            _diagnostics.Roku =
                $"{device.Name} ({device.IpAddress})";
        }

        _diagnostics.Ffmpeg =
            string.IsNullOrWhiteSpace(_ffmpegPath)
                ? "Not found"
                : _ffmpegPath;

        _diagnostics.YtDlp =
            string.IsNullOrWhiteSpace(_ytDlpPath)
                ? "Not found / direct links only"
                : _ytDlpPath;

        if (hls is not null)
            _diagnostics.Hls = hls;

        if (streamUrl is not null)
            _diagnostics.StreamUrl = streamUrl;

        if (message is not null)
            _diagnostics.LastMessage = message;

        DiagnosticsTextBox.Text =
            $"Roku: {_diagnostics.Roku}{Environment.NewLine}" +
            $"FFmpeg: {_diagnostics.Ffmpeg}{Environment.NewLine}" +
            $"yt-dlp: {_diagnostics.YtDlp}{Environment.NewLine}" +
            $"HLS: {_diagnostics.Hls}{Environment.NewLine}" +
            $"Stream: {_diagnostics.StreamUrl}{Environment.NewLine}" +
            $"Last: {_diagnostics.LastMessage}";
    }

    private void RefreshCaptureSources()
    {
        var sources = _captureSources.GetSources();
        CaptureSourceComboBox.ItemsSource = sources;
        if (sources.Count > 0)
            CaptureSourceComboBox.SelectedIndex = 0;
    }

    private void RefreshCaptureSourcesButton_Click(object sender, RoutedEventArgs e) =>
        RefreshCaptureSources();

    private async void RefreshAudioDevicesButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshAudioSourcesAsync();
    }

    private async void StartLiveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_rokuClient is null)
        {
            LiveStatusText.Text = "Select a Roku device first.";
            return;
        }

        if (string.IsNullOrWhiteSpace(_ffmpegPath))
        {
            LiveStatusText.Text = "FFmpeg was not found.";
            return;
        }

        if (CaptureSourceComboBox.SelectedItem is not CaptureSource source)
        {
            LiveStatusText.Text = "Select a capture source.";
            return;
        }

        string friendlyEncoder =
            EncoderComboBox.SelectedItem?.ToString()
            ?? "CPU (libx264)";

        string encoder =
            EncoderDetectionService.ToFFmpegEncoder(
                friendlyEncoder);

        int fps =
            int.Parse(
                ((ComboBoxItem)FpsComboBox.SelectedItem)
                .Content.ToString()!);

        string bitrateText =
            ((ComboBoxItem)BitrateComboBox.SelectedItem)
            .Content.ToString()!;

        int bitrate =
            int.Parse(
                bitrateText.Split(' ')[0]);

        string? audioName = null;

        if (IncludeAudioCheckBox.IsChecked == true &&
            AudioDeviceComboBox.SelectedItem is AudioSourceOption audio)
        {
            if (audio.Kind != AudioSourceKind.None)
                audioName = audio.DeviceName;
        }

        StartLiveButton.IsEnabled = false;

        try
        {
            await StopLiveInternalAsync(false);

            LiveStatusText.Text =
                $"Starting {source.Name}...";

            await _liveCapture.StartAsync(
                _ffmpegPath,
                source,
                encoder,
                audioName,
                fps,
                bitrate);

            await _liveServer.StartAsync(
                _liveCapture.OutputDirectory);

            var ip =
                NetworkHelper.GetBestLocalIPv4ForRemote(
                    _rokuClient.Device.IpAddress)
                ?? throw new InvalidOperationException(
                    "Could not determine the PC LAN IP.");

            string url =
                $"http://{ip}:{_liveServer.Port}/live/index.m3u8";

            LiveStreamUrlTextBox.Text = url;
            SetPcAudioMonitorSource(url);

            LiveStatusText.Text =
                "Launching Dowe LanCaster receiver...";

            await _rokuClient
                .LaunchDoweLanCasterLiveAsync(url);

            StopLiveButton.IsEnabled = true;
            StatusText.Text = "Live casting active.";

            LiveStatusText.Text =
                $"Live casting {source.Name} at {fps} FPS " +
                $"using {friendlyEncoder}" +
                (string.IsNullOrWhiteSpace(audioName)
                    ? " without audio."
                    : " with PC audio.");

            SaveCurrentSettings();

            UpdateDiagnostics(
                hls: "Live Cast running",
                streamUrl: url,
                message: LiveStatusText.Text);
        }
        catch (Exception ex)
        {
            await StopLiveInternalAsync(false);

            LiveStatusText.Text =
                $"Live cast failed: {ex.Message}";

            StatusText.Text =
                "Live cast failed.";

            UpdateDiagnostics(
                hls: "Live Cast failed",
                message: ex.Message);
        }
        finally
        {
            StartLiveButton.IsEnabled = true;
        }
    }

    private async void StopLiveButton_Click(object sender, RoutedEventArgs e) =>
        await StopLiveInternalAsync(true);

    private async Task StopLiveInternalAsync(bool sendHome)
    {
        StopLiveButton.IsEnabled = false;

        if (sendHome && _rokuClient is not null)
        {
            try { await _rokuClient.SendKeyAsync("Home"); }
            catch { }
        }

        await _liveServer.StopAsync();
        await _liveCapture.StopAsync();
        LiveStreamUrlTextBox.Clear();
        await StopPcAudioMonitorAsync();

        if (sendHome)
        {
            LiveStatusText.Text = "Live casting stopped.";
            StatusText.Text = "Ready.";
        }
    }

    private async void ScanButton_Click(object sender, RoutedEventArgs e)
    {
        ScanButton.IsEnabled = false;

        StatusText.Text =
            "Scanning for Roku devices via SSDP and LAN fallback...";

        try
        {
            var devices =
                await _discoveryService.DiscoverAsync(
                    TimeSpan.FromSeconds(4));

            DeviceComboBox.ItemsSource = devices;

            if (devices.Count == 0)
            {
                StatusText.Text =
                    "No Roku devices found. Use Add Roku by IP if needed.";

                UpdateDiagnostics(
                    message: "Automatic Roku discovery found no devices.");

                return;
            }

            var preferred =
                devices.FirstOrDefault(x =>
                    x.IpAddress.Equals(
                        _settings.LastRokuIp,
                        StringComparison.OrdinalIgnoreCase));

            DeviceComboBox.SelectedItem =
                preferred ?? devices[0];

            StatusText.Text =
                $"Found {devices.Count} Roku device(s).";

            UpdateDiagnostics(
                message: $"Automatic discovery found {devices.Count} Roku device(s).");
        }
        catch (Exception ex)
        {
            StatusText.Text =
                $"Discovery failed: {ex.Message}";

            UpdateDiagnostics(
                message: ex.Message);
        }
        finally
        {
            ScanButton.IsEnabled = true;
        }
    }

    private async void DeviceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _rokuClient?.Dispose();
        _rokuClient = null;
        AppsListBox.ItemsSource = null;
        AppsGridListBox.ItemsSource = null;

        if (DeviceComboBox.SelectedItem is not RokuDevice device)
            return;

        _rokuClient = new RokuClient(device);

        ManualRokuIpTextBox.Text =
            device.IpAddress;

        _settings.LastRokuIp =
            device.IpAddress;

        _settingsService.Save(_settings);

        StatusText.Text =
            $"Connected to {device.Name} at {device.IpAddress}.";

        CastStatusText.Text =
            $"Ready to cast to {device.Name}.";

        LiveStatusText.Text =
            $"Ready to live cast to {device.Name}.";

        LinkStatusText.Text =
            $"Ready to stream a link to {device.Name}.";

        UpdateDiagnostics(
            message: $"Connected to Roku {device.IpAddress}.");

        await LoadAppsAsync();
    }

    private void ChooseFolderButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        using var dialog =
            new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Choose a folder of videos to stream",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = false,
                SelectedPath =
                    Directory.Exists(FolderPathTextBox.Text)
                        ? FolderPathTextBox.Text
                        : ""
            };

        if (dialog.ShowDialog() !=
            System.Windows.Forms.DialogResult.OK)
        {
            return;
        }

        FolderPathTextBox.Text =
            dialog.SelectedPath;

        _settings.LastFolderPath =
            dialog.SelectedPath;

        _settingsService.Save(_settings);

        ScanFolder(dialog.SelectedPath);
    }

    private void ScanFolder(string folder)
    {
        try
        {
            _folderItems =
                _folderPlaylistService.Scan(
                    folder,
                    FolderIncludeSubfoldersCheckBox.IsChecked == true)
                .ToList();

            FolderPlaylistListBox.ItemsSource =
                _folderItems;

            FolderCountText.Text =
                $"{_folderItems.Count} video" +
                (_folderItems.Count == 1 ? "" : "s");

            FolderProgressText.Text =
                $"0 of {_folderItems.Count}";

            FolderPlayButton.IsEnabled =
                _folderItems.Count > 0;

            FolderPreviousButton.IsEnabled =
                _folderItems.Count > 1;

            FolderNextButton.IsEnabled =
                _folderItems.Count > 1;

            FolderNowPlayingText.Text =
                _folderItems.Count == 0
                    ? "No supported videos found"
                    : "Ready";

            _folderIndex = -1;

            UpdateDiagnostics(
                message:
                    $"Folder scan found {_folderItems.Count} video(s).");
        }
        catch (Exception ex)
        {
            FolderNowPlayingText.Text =
                $"Folder scan failed: {ex.Message}";

            UpdateDiagnostics(
                message: ex.Message);
        }
    }

    private void RescanFolderButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(
                FolderPathTextBox.Text))
        {
            ScanFolder(
                FolderPathTextBox.Text);
        }
    }

    private void FolderOptionChanged(
        object sender,
        RoutedEventArgs e)
    {
        if (IsLoaded &&
            !string.IsNullOrWhiteSpace(
                FolderPathTextBox.Text) &&
            Directory.Exists(
                FolderPathTextBox.Text))
        {
            ScanFolder(
                FolderPathTextBox.Text);
        }
    }

    private void ShuffleFolderButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_folderItems.Count < 2)
            return;

        var random = Random.Shared;

        for (var i = _folderItems.Count - 1;
             i > 0;
             i--)
        {
            var j =
                random.Next(i + 1);

            (_folderItems[i], _folderItems[j]) =
                (_folderItems[j], _folderItems[i]);
        }

        FolderPlaylistService.Renumber(
            _folderItems);

        RefreshFolderPlaylistView();

        _folderIndex = -1;

        FolderNowPlayingText.Text =
            "Playlist shuffled.";

        UpdateDiagnostics(
            message: "Folder playlist shuffled.");
    }

    private void FolderMoveUpButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (FolderPlaylistListBox.SelectedItem
            is not FolderMediaItem item)
        {
            return;
        }

        var index =
            _folderItems.IndexOf(item);

        if (index <= 0)
            return;

        (_folderItems[index - 1], _folderItems[index]) =
            (_folderItems[index], _folderItems[index - 1]);

        FolderPlaylistService.Renumber(
            _folderItems);

        RefreshFolderPlaylistView();

        FolderPlaylistListBox.SelectedItem =
            item;

        if (_folderIndex == index)
            _folderIndex = index - 1;
        else if (_folderIndex == index - 1)
            _folderIndex = index;

        UpdateDiagnostics(
            message: "Moved playlist item up.");
    }

    private void FolderMoveDownButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (FolderPlaylistListBox.SelectedItem
            is not FolderMediaItem item)
        {
            return;
        }

        var index =
            _folderItems.IndexOf(item);

        if (index < 0 ||
            index >= _folderItems.Count - 1)
        {
            return;
        }

        (_folderItems[index + 1], _folderItems[index]) =
            (_folderItems[index], _folderItems[index + 1]);

        FolderPlaylistService.Renumber(
            _folderItems);

        RefreshFolderPlaylistView();

        FolderPlaylistListBox.SelectedItem =
            item;

        if (_folderIndex == index)
            _folderIndex = index + 1;
        else if (_folderIndex == index + 1)
            _folderIndex = index;

        UpdateDiagnostics(
            message: "Moved playlist item down.");
    }

    private void FolderSortAscendingButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        SortFolderPlaylist(
            descending: false);
    }

    private void FolderSortDescendingButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        SortFolderPlaylist(
            descending: true);
    }

    private void SortFolderPlaylist(
        bool descending)
    {
        FolderMediaItem? current =
            _folderIndex >= 0 &&
            _folderIndex < _folderItems.Count
                ? _folderItems[_folderIndex]
                : null;

        _folderItems =
            (descending
                ? _folderItems.OrderByDescending(
                    x => x.FileName,
                    StringComparer.OrdinalIgnoreCase)
                : _folderItems.OrderBy(
                    x => x.FileName,
                    StringComparer.OrdinalIgnoreCase))
            .ToList();

        FolderPlaylistService.Renumber(
            _folderItems);

        if (current is not null)
            _folderIndex =
                _folderItems.IndexOf(current);

        RefreshFolderPlaylistView();

        UpdateDiagnostics(
            message:
                descending
                    ? "Folder playlist sorted Z-A."
                    : "Folder playlist sorted A-Z.");
    }

    private void RefreshFolderPlaylistView()
    {
        FolderPlaylistListBox.ItemsSource = null;
        FolderPlaylistListBox.ItemsSource =
            _folderItems;
    }

    private async void FolderPlaylistListBox_MouseDoubleClick(
        object sender,
        MouseButtonEventArgs e)
    {
        if (FolderPlaylistListBox.SelectedItem
            is not FolderMediaItem item)
        {
            return;
        }

        var index =
            _folderItems.IndexOf(item);

        if (index >= 0)
            await PlayFolderIndexAsync(index);
    }

    private async void FolderPlayButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_folderItems.Count == 0)
            return;

        int index;

        if (FolderPlaylistListBox.SelectedItem
            is FolderMediaItem selected)
        {
            index =
                _folderItems.IndexOf(selected);
        }
        else if (_folderIndex >= 0)
        {
            index = _folderIndex;
        }
        else
        {
            index = 0;
        }

        await PlayFolderIndexAsync(index);
    }

    private async void FolderPreviousButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_folderItems.Count == 0)
            return;

        var next =
            _folderIndex <= 0
                ? _folderItems.Count - 1
                : _folderIndex - 1;

        await PlayFolderIndexAsync(next);
    }

    private async void FolderNextButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        await AdvanceFolderAsync(
            manual: true);
    }

    private async void FolderStopButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        await StopFolderInternalAsync(
            sendHome: true);

        FolderNowPlayingText.Text =
            "Folder playback stopped.";

        UpdateDiagnostics(
            hls: "Folder Cast stopped",
            streamUrl: "",
            message: "Folder playback stopped.");
    }

    private async Task PlayFolderIndexAsync(
        int index)
    {
        if (_rokuClient is null)
        {
            FolderNowPlayingText.Text =
                "Select a Roku device first.";
            return;
        }

        if (string.IsNullOrWhiteSpace(
                _ffmpegPath))
        {
            FolderNowPlayingText.Text =
                "FFmpeg was not found.";
            return;
        }

        if (index < 0 ||
            index >= _folderItems.Count)
        {
            return;
        }

        var item =
            _folderItems[index];

        FolderPlayButton.IsEnabled =
            false;

        try
        {
            _folderPollTimer.Stop();

            var launchReceiver = !_folderReceiverLaunched;

            if (launchReceiver)
            {
                await StopFolderInternalAsync(
                    sendHome: false);
            }
            else
            {
                await _folderTranscoder.StopAsync();
            }

            _folderIndex = index;
            _folderSawPlaying = false;

            FolderPlaylistListBox.SelectedItem =
                item;

            FolderPlaylistListBox.ScrollIntoView(
                item);

            FolderNowPlayingText.Text =
                $"Preparing: {item.FileName}";

            FolderProgressText.Text =
                $"{index + 1} of {_folderItems.Count}";

            string friendlyEncoder =
                FolderEncoderComboBox.SelectedItem?.ToString()
                ?? "CPU (libx264)";

            string encoder =
                EncoderDetectionService
                .ToFFmpegEncoder(
                    friendlyEncoder);

            string bitrateText =
                ((ComboBoxItem)
                    FolderBitrateComboBox
                    .SelectedItem)
                .Content.ToString()!;

            int bitrate =
                int.Parse(
                    bitrateText.Split(' ')[0]);

            await _folderTranscoder.StartAsync(
                _ffmpegPath,
                item.FilePath,
                encoder,
                bitrate);

            await _folderServer.StartAsync(
                _folderTranscoder.OutputDirectory,
                port: 8768);

            var ip =
                NetworkHelper
                .GetBestLocalIPv4ForRemote(
                    _rokuClient.Device.IpAddress)
                ?? throw new InvalidOperationException(
                    "Could not determine the PC LAN IP.");

            var streamUrl =
                $"http://{ip}:{_folderServer.Port}/live/index.m3u8" +
                $"?item={Guid.NewGuid():N}";
            SetPcAudioMonitorSource(streamUrl);

            _folderServer.SetControlState(streamUrl);

            if (launchReceiver)
            {
                var controlUrl =
                    $"http://{ip}:{_folderServer.Port}/control";

                await _rokuClient
                    .LaunchDoweLanCasterLiveAsync(
                        streamUrl,
                        controlUrl);

                _folderReceiverLaunched = true;
            }

            FolderNowPlayingText.Text =
                $"Now playing: {item.FileName}";

            FolderStopButton.IsEnabled = true;
            FolderPreviousButton.IsEnabled =
                _folderItems.Count > 1;
            FolderNextButton.IsEnabled =
                _folderItems.Count > 1;

            _folderPollTimer.Start();

            SaveCurrentSettings();

            UpdateDiagnostics(
                hls: "Folder Cast running",
                streamUrl: streamUrl,
                message:
                    $"Folder item {index + 1}/{_folderItems.Count}: {item.FileName}");
        }
        catch (Exception ex)
        {
            FolderNowPlayingText.Text =
                $"Could not play {item.FileName}: {ex.Message}";

            UpdateDiagnostics(
                hls: "Folder Cast item failed",
                message: ex.Message);

            // Skip a bad item instead of killing the entire playlist.
            if (_folderItems.Count > 1)
            {
                await Task.Delay(600);
                await AdvanceFolderAsync(
                    manual: false);
            }
        }
        finally
        {
            FolderPlayButton.IsEnabled =
                _folderItems.Count > 0;
        }
    }

    private async Task AdvanceFolderAsync(
        bool manual)
    {
        if (_folderAdvanceInProgress ||
            _folderItems.Count == 0)
        {
            return;
        }

        _folderAdvanceInProgress = true;

        try
        {
            var repeat =
                ((ComboBoxItem?)
                    FolderRepeatComboBox
                    .SelectedItem)
                ?.Content?.ToString()
                ?? "Off";

            if (!manual &&
                repeat.Equals(
                    "One",
                    StringComparison.OrdinalIgnoreCase) &&
                _folderIndex >= 0)
            {
                await PlayFolderIndexAsync(
                    _folderIndex);
                return;
            }

            int next;

            if (FolderShuffleCheckBox.IsChecked == true &&
                _folderItems.Count > 1)
            {
                do
                {
                    next =
                        Random.Shared.Next(
                            _folderItems.Count);
                }
                while (next == _folderIndex);
            }
            else
            {
                next =
                    _folderIndex + 1;
            }

            if (next >= _folderItems.Count)
            {
                if (repeat.Equals(
                        "All",
                        StringComparison.OrdinalIgnoreCase) ||
                    manual)
                {
                    next = 0;
                }
                else
                {
                    await StopFolderInternalAsync(
                        sendHome: false);

                    FolderNowPlayingText.Text =
                        "Playlist finished.";

                    FolderProgressText.Text =
                        $"{_folderItems.Count} of {_folderItems.Count}";

                    UpdateDiagnostics(
                        hls: "Folder Cast finished",
                        streamUrl: "",
                        message: "Folder playlist finished.");

                    return;
                }
            }

            await PlayFolderIndexAsync(next);
        }
        finally
        {
            _folderAdvanceInProgress = false;
        }
    }

    private async void FolderPollTimer_Tick(
        object? sender,
        EventArgs e)
    {
        if (_rokuClient is null ||
            _folderIndex < 0)
        {
            return;
        }

        try
        {
            var state =
                await _rokuClient
                    .GetMediaPlayerStateAsync();

            if (state.IsPlaying)
            {
                _folderSawPlaying = true;
                return;
            }

            if (_folderSawPlaying &&
                state.IsStopped)
            {
                _folderSawPlaying = false;
                _folderPollTimer.Stop();

                if (FolderAutoPlayCheckBox.IsChecked == true)
                {
                    await AdvanceFolderAsync(
                        manual: false);
                }
                else
                {
                    FolderNowPlayingText.Text =
                        "Playback finished.";
                    FolderStopButton.IsEnabled = false;

                    UpdateDiagnostics(
                        hls: "Folder Cast item finished",
                        streamUrl: "",
                        message: "Auto-play next is disabled.");
                }
            }
        }
        catch
        {
            // A transient ECP polling error should not stop playback.
        }
    }

    private async Task StopFolderInternalAsync(
        bool sendHome)
    {
        _folderPollTimer.Stop();
        _folderSawPlaying = false;
        _folderReceiverLaunched = false;
        _folderServer.SetControlState(null);

        if (sendHome &&
            _rokuClient is not null)
        {
            try
            {
                await _rokuClient
                    .SendKeyAsync("Home");
            }
            catch
            {
            }
        }

        await _folderServer.StopAsync();
        await _folderTranscoder.StopAsync();
        await StopPcAudioMonitorAsync();

        FolderStopButton.IsEnabled = false;
    }

    private void ChooseFileButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Choose media to cast",
            Filter = "Media files|*.mp4;*.m4v;*.mov;*.mkv;*.webm;*.mp3;*.m4a;*.aac|All files|*.*"
        };

        if (dialog.ShowDialog() != true)
            return;

        _selectedFile = dialog.FileName;
        SelectedFileTextBox.Text = _selectedFile;
        CastStatusText.Text = $"Selected: {Path.GetFileName(_selectedFile)}";
    }

    private async void CastButton_Click(object sender, RoutedEventArgs e)
    {
        if (_rokuClient is null)
        {
            CastStatusText.Text = "Select a Roku first.";
            return;
        }

        if (_selectedFile is null || !File.Exists(_selectedFile))
        {
            CastStatusText.Text = "Choose a media file first.";
            return;
        }

        CastButton.IsEnabled = false;

        try
        {
            await _mediaServer.StartAsync(_selectedFile);

            var ip = NetworkHelper.GetBestLocalIPv4ForRemote(_rokuClient.Device.IpAddress)
                ?? throw new InvalidOperationException("Could not determine the PC LAN address.");

            var streamUrl = $"http://{ip}:{_mediaServer.Port}/media";
            StreamUrlTextBox.Text = streamUrl;
            SetPcAudioMonitorSource(streamUrl);

            await _rokuClient.LaunchDoweLanCasterAsync(streamUrl);

            CastStatusText.Text =
                $"Casting {Path.GetFileName(_selectedFile)} to {_rokuClient.Device.Name}.";

            StopCastingButton.IsEnabled = true;
            StatusText.Text = "File casting active.";
        }
        catch (Exception ex)
        {
            await _mediaServer.StopAsync();
            CastStatusText.Text = $"Casting failed: {ex.Message}";
            StatusText.Text = "Casting failed.";
        }
        finally
        {
            CastButton.IsEnabled = true;
        }
    }

    private async void StopCastingButton_Click(object sender, RoutedEventArgs e)
    {
        StopCastingButton.IsEnabled = false;
        try
        {
            if (_rokuClient is not null)
                await _rokuClient.SendKeyAsync("Home");
        }
        catch { }

        await _mediaServer.StopAsync();
        StreamUrlTextBox.Clear();
        await StopPcAudioMonitorAsync();
        CastStatusText.Text = "Casting stopped.";
        StatusText.Text = "Ready.";
    }

    private async void RemoteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_rokuClient is null || sender is not System.Windows.Controls.Button b || b.Tag is not string key)
            return;

        try
        {
            await SendRemoteKeyAsync(key);
            StatusText.Text = $"Sent: {key}";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Remote command failed: {ex.Message}";
        }
    }

    private void OpenRemoteWindowButton_Click(object sender, RoutedEventArgs e)
    {
        if (_rokuClient is null)
        {
            StatusText.Text = "Select a Roku device first.";
            return;
        }

        if (_remoteWindow is not null)
        {
            _remoteWindow.Activate();
            return;
        }

        _remoteWindow = new RemoteWindow(
            _rokuClient.Device.Name,
            SendRemoteKeyAsync,
            SetRokuVolumeAsync)
        {
            Owner = this
        };
        _remoteWindow.Closed += (_, _) => _remoteWindow = null;
        _remoteWindow.Show();
    }

    private Task SendRemoteKeyAsync(string key)
    {
        return _rokuClient?.SendKeyAsync(key)
            ?? Task.FromException(
                new InvalidOperationException("Select a Roku device first."));
    }

    private Task SetRokuVolumeAsync(int level)
    {
        return _rokuClient?.SetVolumeAsync(level)
            ?? Task.FromException(
                new InvalidOperationException("Select a Roku device first."));
    }

    private async void SetVolumeButton_Click(object sender, RoutedEventArgs e)
    {
        if (_rokuClient is null)
        {
            StatusText.Text = "Select a Roku device first.";
            return;
        }

        if (!int.TryParse(VolumeLevelTextBox.Text, out var level) ||
            level is < 0 or > 100)
        {
            StatusText.Text = "Enter a Roku volume from 0 to 100.";
            return;
        }

        try
        {
            StatusText.Text = $"Setting Roku volume to {level}...";
            await SetRokuVolumeAsync(level);
            StatusText.Text = $"Roku volume set to {level}.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Could not set Roku volume: {ex.Message}";
        }
    }

    private async void HeadphoneModeButton_Click(object sender, RoutedEventArgs e)
    {
        if (_pcAudioMonitor.IsPlaying)
        {
            await StopPcAudioMonitorAsync();
            return;
        }

        if (string.IsNullOrWhiteSpace(_pcAudioMonitorUrl))
        {
            HeadphoneStatusText.Text =
                "Start a Link, Live, Folder, or File cast before enabling headphone mode.";
            return;
        }

        try
        {
            await _pcAudioMonitor.StartAsync(_pcAudioMonitorUrl);
            HeadphoneModeButton.Content = "🎧  Stop Listening on This PC";
            HeadphoneStatusText.Text =
                "Playing the current Dowe LanCaster stream through your Windows default output.";
        }
        catch (Exception ex)
        {
            HeadphoneStatusText.Text =
                $"Headphone mode could not start: {ex.Message}";
        }
    }

    private void SetPcAudioMonitorSource(string streamUrl)
    {
        _pcAudioMonitorUrl = streamUrl;
    }

    private async Task StopPcAudioMonitorAsync()
    {
        await _pcAudioMonitor.StopAsync();
        _pcAudioMonitorUrl = null;
        HeadphoneModeButton.Content = "🎧  Listen on This PC";
        HeadphoneStatusText.Text =
            "Headphone mode is off. Select your headphones as the Windows default output.";
    }

    private async void SendTextButton_Click(object sender, RoutedEventArgs e)
    {
        if (_rokuClient is null || string.IsNullOrEmpty(RokuTextInput.Text))
            return;

        try
        {
            await _rokuClient.SendTextAsync(RokuTextInput.Text);
            RokuTextInput.Clear();
            StatusText.Text = "Text sent.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Text failed: {ex.Message}";
        }
    }

    private void RokuTextInput_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        RokuTextPlaceholder.Visibility =
            string.IsNullOrWhiteSpace(RokuTextInput.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
    }

    private void DarkModeCheckBox_Changed(
        object sender,
        RoutedEventArgs e)
    {
        var dark =
            DarkModeCheckBox.IsChecked == true;

        ThemeService.ApplyTheme(dark);

        _settings.UseDarkMode =
            dark;

        _settingsService.Save(_settings);

        UpdateDiagnostics(
            message:
                dark
                    ? "Dark mode enabled."
                    : "Light mode enabled.");
    }

    private void VoiceControlButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_voiceListening)
        {
            StopVoiceControl();
            return;
        }

        if (_rokuClient is null)
        {
            VoiceStatusText.Text =
                "Select a Roku device first.";
            return;
        }

        try
        {
            if (_voiceRecognizer is null)
            {
                if (SpeechRecognitionEngine
                    .InstalledRecognizers()
                    .Count == 0)
                {
                    VoiceStatusText.Text =
                        "No Windows speech recognizer is installed.";
                    return;
                }

                _voiceRecognizer =
                    new SpeechRecognitionEngine();

                var choices =
                    new Choices();

                choices.Add(
                    VoiceCommandMap.Keys.ToArray());

                var grammarBuilder =
                    new GrammarBuilder();

                grammarBuilder.Append(choices);

                _voiceRecognizer.LoadGrammar(
                    new Grammar(grammarBuilder));

                _voiceRecognizer.SetInputToDefaultAudioDevice();

                _voiceRecognizer.SpeechRecognized +=
                    VoiceRecognizer_SpeechRecognized;

                _voiceRecognizer.RecognizeCompleted +=
                    VoiceRecognizer_RecognizeCompleted;
            }

            _voiceRecognizer.RecognizeAsync(
                RecognizeMode.Multiple);

            _voiceListening = true;

            VoiceControlButton.Content =
                "🎤  Stop Voice Control";

            VoiceStatusText.Text =
                "Listening: say Home, Back, OK, Play, Volume Up, and more.";

            UpdateDiagnostics(
                message: "Voice control started.");
        }
        catch (Exception ex)
        {
            VoiceStatusText.Text =
                $"Voice control failed: {ex.Message}";

            UpdateDiagnostics(
                message: ex.Message);
        }
    }

    private async void VoiceRecognizer_SpeechRecognized(
        object? sender,
        SpeechRecognizedEventArgs e)
    {
        if (e.Result.Confidence < 0.55)
        {
            await Dispatcher.InvokeAsync(
                () =>
                    VoiceStatusText.Text =
                        $"Heard \"{e.Result.Text}\" but confidence was too low.");

            return;
        }

        if (!VoiceCommandMap.TryGetValue(
                e.Result.Text,
                out var key))
        {
            return;
        }

        var roku =
            _rokuClient;

        if (roku is null)
            return;

        try
        {
            await Dispatcher.InvokeAsync(
                () =>
                    VoiceStatusText.Text =
                        $"Heard: {e.Result.Text}");

            await roku.SendKeyAsync(key);

            await Dispatcher.InvokeAsync(
                () =>
                {
                    StatusText.Text =
                        $"Voice command sent: {e.Result.Text}";

                    UpdateDiagnostics(
                        message:
                            $"Voice remote command: {e.Result.Text}.");
                });
        }
        catch (Exception ex)
        {
            await Dispatcher.InvokeAsync(
                () =>
                    VoiceStatusText.Text =
                        $"Voice command failed: {ex.Message}");
        }
    }

    private void VoiceRecognizer_RecognizeCompleted(
        object? sender,
        RecognizeCompletedEventArgs e)
    {
        Dispatcher.Invoke(
            () =>
            {
                _voiceListening = false;

                VoiceControlButton.Content =
                    "🎤  Start Voice Control";

                VoiceStatusText.Text =
                    e.Error is null
                        ? "Voice control is off"
                        : $"Voice recognition stopped: {e.Error.Message}";
            });
    }

    private void StopVoiceControl()
    {
        if (_voiceRecognizer is not null)
        {
            try
            {
                _voiceRecognizer.RecognizeAsyncCancel();
            }
            catch
            {
            }
        }

        _voiceListening = false;

        if (VoiceControlButton is not null)
            VoiceControlButton.Content =
                "🎤  Start Voice Control";

        if (VoiceStatusText is not null)
            VoiceStatusText.Text =
                "Voice control is off";

        UpdateDiagnostics(
            message: "Voice control stopped.");
    }

    private async void RefreshAppsButton_Click(object sender, RoutedEventArgs e) =>
        await LoadAppsAsync();

    private async Task LoadAppsAsync()
    {
        if (_rokuClient is null) return;

        try
        {
            var apps = await _rokuClient.GetAppsAsync();
            AppsListBox.ItemsSource = apps;
            AppsGridListBox.ItemsSource = apps;
            StatusText.Text = $"Loaded {apps.Count} installed apps.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Could not load apps: {ex.Message}";
        }
    }

    private async void LaunchSelectedAppButton_Click(object sender, RoutedEventArgs e) =>
        await LaunchSelectedAppAsync();

    private async void AppsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e) =>
        await LaunchSelectedAppAsync();

    private void AppsDisplayMode_Changed(object sender, RoutedEventArgs e)
    {
        if (AppsListBox is null || AppsGridListBox is null)
            return;

        var showGrid = AppsGridViewButton.IsChecked == true;
        AppsListBox.Visibility = showGrid ? Visibility.Collapsed : Visibility.Visible;
        AppsGridListBox.Visibility = showGrid ? Visibility.Visible : Visibility.Collapsed;
    }

    private async Task LaunchSelectedAppAsync()
    {
        var app = AppsListBox.SelectedItem as RokuApp
            ?? AppsGridListBox.SelectedItem as RokuApp;

        if (_rokuClient is null || app is null)
            return;

        try
        {
            await _rokuClient.LaunchAppAsync(app.Id);
            StatusText.Text = $"Launched {app.Name}.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Launch failed: {ex.Message}";
        }
    }

    private async void CheckForUpdatesButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        CheckForUpdatesButton.IsEnabled = false;
        CheckForUpdatesButton.Content = "Checking...";
        StatusText.Text = "Checking for updates...";

        try
        {
            var update = await _updateService.CheckForUpdateAsync();

            if (!update.IsUpdateAvailable)
            {
                StatusText.Text =
                    $"Dowe LanCaster v{update.CurrentVersion} is up to date.";

                System.Windows.MessageBox.Show(
                    this,
                    $"You already have the latest version: v{update.CurrentVersion}.",
                    "Dowe LanCaster Update",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            if (string.IsNullOrWhiteSpace(update.InstallerDownloadUrl))
            {
                throw new InvalidOperationException(
                    $"Version v{update.LatestVersion} does not include a Windows installer.");
            }

            var choice = System.Windows.MessageBox.Show(
                this,
                $"Dowe LanCaster v{update.LatestVersion} is available.\n\n" +
                $"Installed version: v{update.CurrentVersion}\n\n" +
                "Download and install it now? The application will close and reopen automatically.",
                "Dowe LanCaster Update Available",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (choice != MessageBoxResult.Yes)
            {
                StatusText.Text =
                    $"Update v{update.LatestVersion} is available.";
                return;
            }

            var progress = new Progress<double>(percentage =>
            {
                CheckForUpdatesButton.Content =
                    $"Downloading {percentage:0}%";
                StatusText.Text =
                    $"Downloading Dowe LanCaster v{update.LatestVersion}: {percentage:0}%";
            });

            var installerPath = await _updateService.DownloadInstallerAsync(
                update,
                progress);

            StatusText.Text =
                "Starting the update installer...";
            CheckForUpdatesButton.Content = "Installing...";

            UpdateService.StartInstaller(installerPath);
            System.Windows.Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            StatusText.Text =
                $"Update failed: {ex.Message}";

            System.Windows.MessageBox.Show(
                this,
                "Dowe LanCaster could not complete the update.\n\n" +
                ex.Message,
                "Update Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            if (CheckForUpdatesButton is not null)
            {
                CheckForUpdatesButton.IsEnabled = true;
                CheckForUpdatesButton.Content = "Check for Updates";
            }
        }
    }

    protected override async void OnClosed(EventArgs e)
    {
        SaveCurrentSettings();

        StopVoiceControl();

        if (_voiceRecognizer is not null)
        {
            _voiceRecognizer.SpeechRecognized -=
                VoiceRecognizer_SpeechRecognized;

            _voiceRecognizer.RecognizeCompleted -=
                VoiceRecognizer_RecognizeCompleted;

            _voiceRecognizer.Dispose();
            _voiceRecognizer = null;
        }

        _folderPollTimer.Stop();
        await _folderServer.DisposeAsync();
        await _folderTranscoder.DisposeAsync();
        await _urlServer.DisposeAsync();
        await _urlCapture.DisposeAsync();
        await _pcAudioMonitor.DisposeAsync();
        await _liveServer.DisposeAsync();
        await _liveCapture.DisposeAsync();
        await _mediaServer.DisposeAsync();

        _rokuClient?.Dispose();

        base.OnClosed(e);
    }
}
