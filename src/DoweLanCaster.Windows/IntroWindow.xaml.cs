using System.IO;
using System.Windows;

namespace DoweLanCaster;

public partial class IntroWindow : Window
{
    private readonly TaskCompletionSource<bool> _finished =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task Completion => _finished.Task;

    public IntroWindow()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            StartIntroVideo();
        };
    }

    private void StartIntroVideo()
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "Resources",
            "intro.mp4");

        if (!File.Exists(path))
        {
            IntroStatusText.Text =
                "Intro video not found. Starting Dowe LanCaster...";

            _ = CompleteAfterFallbackAsync();
            return;
        }

        try
        {
            IntroStatusText.Text =
                "Starting Dowe LanCaster...";

            IntroVideo.Source =
                new Uri(path, UriKind.Absolute);

            IntroVideo.Position =
                TimeSpan.Zero;

            IntroVideo.Play();
        }
        catch
        {
            _ = CompleteAfterFallbackAsync();
        }
    }

    private void IntroVideo_MediaOpened(
        object sender,
        RoutedEventArgs e)
    {
        FallbackImage.Visibility =
            Visibility.Collapsed;
    }

    private void IntroVideo_MediaEnded(
        object sender,
        RoutedEventArgs e)
    {
        Complete();
    }

    private void IntroVideo_MediaFailed(
        object sender,
        ExceptionRoutedEventArgs e)
    {
        IntroStatusText.Text =
            "Intro video could not be played. Starting Dowe LanCaster...";

        _ = CompleteAfterFallbackAsync();
    }

    private async Task CompleteAfterFallbackAsync()
    {
        await Task.Delay(1800);
        Complete();
    }

    private void Complete()
    {
        if (_finished.TrySetResult(true))
        {
            try
            {
                IntroVideo.Stop();
            }
            catch
            {
            }
        }
    }

    public void SetStatus(string status)
    {
        IntroStatusText.Text = status;
    }
}
