namespace DoweLanCaster.Services;

public static class ThemeService
{
    public static void ApplyTheme(bool dark)
    {
        var resources =
            System.Windows.Application.Current.Resources;

        Set(
            resources,
            "AppBackgroundBrush",
            dark ? "#101318" : "#F4F6F8");

        Set(
            resources,
            "SurfaceBrush",
            dark ? "#191D24" : "#FFFFFF");

        Set(
            resources,
            "ControlBackgroundBrush",
            dark ? "#222832" : "#FFFFFF");

        Set(
            resources,
            "ControlHoverBrush",
            dark ? "#2B3440" : "#EDF2F7");

        Set(
            resources,
            "ForegroundBrush",
            dark ? "#F4F7FA" : "#17202A");

        Set(
            resources,
            "SecondaryForegroundBrush",
            dark ? "#AAB6C4" : "#5F6B77");

        Set(
            resources,
            "BorderBrush",
            dark ? "#3C4652" : "#B8C1CA");

        Set(
            resources,
            "AccentBrush",
            "#25C7F0");

        Set(
            resources,
            "SelectionBrush",
            dark ? "#314A59" : "#CDEFFC");

        Set(resources, System.Windows.SystemColors.ControlBrushKey,
            dark ? "#222832" : "#F0F0F0");
        Set(resources, System.Windows.SystemColors.ControlTextBrushKey,
            dark ? "#F4F7FA" : "#17202A");
        Set(resources, System.Windows.SystemColors.WindowBrushKey,
            dark ? "#222832" : "#FFFFFF");
        Set(resources, System.Windows.SystemColors.WindowTextBrushKey,
            dark ? "#F4F7FA" : "#17202A");
        Set(resources, System.Windows.SystemColors.HighlightBrushKey,
            dark ? "#314A59" : "#CDEFFC");
        Set(resources, System.Windows.SystemColors.HighlightTextBrushKey,
            dark ? "#FFFFFF" : "#17202A");
        Set(resources, System.Windows.SystemColors.GrayTextBrushKey,
            dark ? "#8F9AA7" : "#6B737C");
        Set(resources, System.Windows.SystemColors.MenuBrushKey,
            dark ? "#222832" : "#FFFFFF");
        Set(resources, System.Windows.SystemColors.MenuTextBrushKey,
            dark ? "#F4F7FA" : "#17202A");
    }

    private static void Set(
        System.Windows.ResourceDictionary resources,
        object key,
        string color)
    {
        var parsedColor =
            (System.Windows.Media.Color)
            System.Windows.Media.ColorConverter
                .ConvertFromString(color);

        resources[key] =
            new System.Windows.Media.SolidColorBrush(
                parsedColor);
    }
}
