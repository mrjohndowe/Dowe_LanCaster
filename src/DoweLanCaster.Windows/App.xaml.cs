namespace DoweLanCaster;

public partial class App : System.Windows.Application
{
    private async void Application_Startup(
        object sender,
        System.Windows.StartupEventArgs e)
    {
        var intro =
            new IntroWindow();

        intro.Show();

        try
        {
            await intro.Completion;
        }
        catch
        {
            // Never let an intro failure prevent the application from starting.
        }

        var main =
            new MainWindow();

        MainWindow =
            main;

        intro.Close();
        main.Show();
    }
}
