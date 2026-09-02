using System.Windows;
using System.Windows.Controls;

namespace DoweLanCaster;

public partial class RemoteWindow : Window
{
    private readonly Func<string, Task> _sendKeyAsync;
    private readonly Func<int, Task> _setVolumeAsync;

    public RemoteWindow(
        string deviceName,
        Func<string, Task> sendKeyAsync,
        Func<int, Task> setVolumeAsync)
    {
        InitializeComponent();
        DeviceText.Text = deviceName;
        _sendKeyAsync = sendKeyAsync;
        _setVolumeAsync = setVolumeAsync;
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
}
