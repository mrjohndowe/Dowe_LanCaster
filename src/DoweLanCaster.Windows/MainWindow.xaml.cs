using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
    private readonly SettingsService _settingsService = new();
    private readonly DiagnosticState _diagnostics = new();

    private RokuClient? _rokuClient;
    private string? _selectedFile;
    private string? _ffmpegPath;
    private string? _ytDlpPath;
    private ExtractedMedia? _extractedMedia;
    private AppSettings _settings = new();

    public MainWindow()
    {
        InitializeComponent();

        _settings = _settingsService.Load();

        Loaded += async (_, _) =>
        {
            LoadSavedSettings();
            RefreshCaptureSources();
            await InitializeFFmpegAsync();
            InitializeYtDlp();
            await RefreshAudioSourcesAsync();
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

            var preferred =
                encoders.FirstOrDefault(x =>
                    string.Equals(
                        x,
                        _settings.PreferredEncoder,
                        StringComparison.OrdinalIgnoreCase))
                ?? encoders.FirstOrDefault();

            EncoderComboBox.SelectedItem = preferred;
            LinkEncoderComboBox.SelectedItem = preferred;

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

                _extractedMedia = new ExtractedMedia
                {
                    PageUrl = url,
                    MediaUrl = url,
                    Title = Path.GetFileNameWithoutExtension(path),
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
                            StringComparison.OrdinalIgnoreCase)
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

                LinkStatusText.Text =
                    "Direct media detected. Ready to stream to Roku.";

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

            if (string.IsNullOrWhiteSpace(_ytDlpPath) ||
                !File.Exists(_ytDlpPath))
            {
                throw new InvalidOperationException(
                    @"This is a webpage URL. Install tools\yt-dlp\yt-dlp.exe to extract it.");
            }

            LinkStatusText.Text =
                "Webpage detected. Extracting media...";

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
            AudioDeviceComboBox.ItemsSource = null;
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
        CastStatusText.Text = "Casting stopped.";
        StatusText.Text = "Ready.";
    }

    private async void RemoteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_rokuClient is null || sender is not System.Windows.Controls.Button b || b.Tag is not string key)
            return;

        try
        {
            await _rokuClient.SendKeyAsync(key);
            StatusText.Text = $"Sent: {key}";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Remote command failed: {ex.Message}";
        }
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

    private async void RefreshAppsButton_Click(object sender, RoutedEventArgs e) =>
        await LoadAppsAsync();

    private async Task LoadAppsAsync()
    {
        if (_rokuClient is null) return;

        try
        {
            var apps = await _rokuClient.GetAppsAsync();
            AppsListBox.ItemsSource = apps;
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

    private async Task LaunchSelectedAppAsync()
    {
        if (_rokuClient is null || AppsListBox.SelectedItem is not RokuApp app)
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

    protected override async void OnClosed(EventArgs e)
    {
        SaveCurrentSettings();

        await _urlServer.DisposeAsync();
        await _urlCapture.DisposeAsync();
        await _liveServer.DisposeAsync();
        await _liveCapture.DisposeAsync();
        await _mediaServer.DisposeAsync();

        _rokuClient?.Dispose();

        base.OnClosed(e);
    }
}
