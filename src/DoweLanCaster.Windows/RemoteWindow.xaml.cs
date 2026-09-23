using System.Windows;
using System.Windows.Controls;
using DoweLanCaster.Models;
using DoweLanCaster.Services;

namespace DoweLanCaster;

public partial class RemoteWindow : Window
{
    private readonly RokuClient _rokuClient;
    private readonly Func<Task<string>> _togglePrivateListeningAsync;
    private readonly Func<string> _toggleVoiceControl;
    public event Action<RokuAudioDeviceState>? VolumeStateChanged;

    public RemoteWindow(
        RokuDevice device,
        Func<Task<string>> togglePrivateListeningAsync,
        Func<string> toggleVoiceControl)
    {
        InitializeComponent();
        DeviceText.Text = device.Name;
        _rokuClient = new RokuClient(device);
        _togglePrivateListeningAsync = togglePrivateListeningAsync;
        _toggleVoiceControl = toggleVoiceControl;
        Loaded += async (_, _) => await RefreshAudioStateAsync();
    }

    private async void RemoteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button { Tag: string key })
            return;

        try
        {
            await _rokuClient.SendKeyAsync(key);
            if (key is "VolumeUp" or "VolumeDown" or "VolumeMute")
            {
                await Task.Delay(250);
                await RefreshAudioStateAsync();
            }
            StatusText.Text = $"Sent: {key}";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Remote command failed: {ex.Message}";
        }
    }

    private async void SetVolumeButton_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(VolumeTextBox.Text, out var level) ||
            level is < 0 or > 100)
        {
            StatusText.Text = "Enter a Roku volume from 0 to 100.";
            return;
        }

        try
        {
            StatusText.Text = $"Setting Roku volume to {level}...";
            await _rokuClient.SetVolumeAsync(level);
            await Task.Delay(250);
            await RefreshAudioStateAsync();
            StatusText.Text = $"Roku volume is {VolumeTextBox.Text}.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Could not set Roku volume: {ex.Message}";
        }
    }

    private async void PrivateListeningButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            StatusText.Text = "Connecting Roku audio to this PC...";
            var status = await _togglePrivateListeningAsync();
            PrivateListeningButton.Content =
                status == "Private Listening is off."
                    ? "🎧  Start Roku Private Listening"
                    : "🎧  Stop Roku Private Listening";
            StatusText.Text = status;
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Private Listening could not start: {ex.Message}";
        }
    }

    private async void SendTextButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(RokuTextInput.Text))
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

    private void VoiceControlButton_Click(object sender, RoutedEventArgs e)
    {
        var status = _toggleVoiceControl();
        VoiceControlButton.Content = status.Contains("off", StringComparison.OrdinalIgnoreCase)
            ? "🎤  Start Voice Control"
            : "🎤  Stop Voice Control";
        StatusText.Text = status;
    }

    private async Task RefreshAudioStateAsync()
    {
        try
        {
            var state = await _rokuClient.GetAudioDeviceStateAsync();
            ApplyAudioState(state);
            VolumeStateChanged?.Invoke(state);
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Could not read Roku volume: {ex.Message}";
        }
    }

    public void ApplyAudioState(RokuAudioDeviceState state)
    {
        if (state.Volume is int volume)
            VolumeTextBox.Text = volume.ToString();
    }

    public void ApplyPrivateListeningState(bool active, string status)
    {
        PrivateListeningButton.Content = active
            ? "🎧  Stop Roku Private Listening"
            : "🎧  Start Roku Private Listening";
        StatusText.Text = status == "Stopped"
            ? "Private Listening is off. Java and FFplay were released."
            : $"Private Listening: {status}.";
    }

    protected override void OnClosed(EventArgs e)
    {
        _rokuClient.Dispose();
        base.OnClosed(e);
    }
}
