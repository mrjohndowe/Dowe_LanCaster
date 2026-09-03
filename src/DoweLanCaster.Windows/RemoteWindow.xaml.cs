using System.Windows;
using System.Windows.Controls;

namespace DoweLanCaster;

public partial class RemoteWindow : Window
{
    private readonly Func<string, Task> _sendKeyAsync;
    private readonly Func<int, Task> _setVolumeAsync;
    private readonly Func<Task<string>> _togglePrivateListeningAsync;
    private readonly Func<string, Task> _sendTextAsync;
    private readonly Func<string> _toggleVoiceControl;

    public RemoteWindow(
        string deviceName,
        Func<string, Task> sendKeyAsync,
        Func<int, Task> setVolumeAsync,
        Func<Task<string>> togglePrivateListeningAsync,
        Func<string, Task> sendTextAsync,
        Func<string> toggleVoiceControl)
    {
        InitializeComponent();
        DeviceText.Text = deviceName;
        _sendKeyAsync = sendKeyAsync;
        _setVolumeAsync = setVolumeAsync;
        _togglePrivateListeningAsync = togglePrivateListeningAsync;
        _sendTextAsync = sendTextAsync;
        _toggleVoiceControl = toggleVoiceControl;
    }

    private async void RemoteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button { Tag: string key })
            return;

        try
        {
            await _sendKeyAsync(key);
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
            await _setVolumeAsync(level);
            StatusText.Text = $"Roku volume set to {level}.";
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
            await _sendTextAsync(RokuTextInput.Text);
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
}
