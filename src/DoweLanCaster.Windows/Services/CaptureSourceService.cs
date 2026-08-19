using System.Runtime.InteropServices;
using System.Text;
using DoweLanCaster.Models;
using Forms = System.Windows.Forms;

namespace DoweLanCaster.Services;

public sealed class CaptureSourceService
{
    public IReadOnlyList<CaptureSource> GetSources()
    {
        var list = new List<CaptureSource>
        {
            new() { Type = CaptureSourceType.Desktop, Name = "Entire Desktop" }
        };

        var screens = Forms.Screen.AllScreens;
        for (int i = 0; i < screens.Length; i++)
        {
            var b = screens[i].Bounds;
            list.Add(new CaptureSource
            {
                Type = CaptureSourceType.Monitor,
                Name = $"Monitor {i + 1} ({b.Width}x{b.Height})" + (screens[i].Primary ? " - Primary" : ""),
                Left = b.Left, Top = b.Top, Width = b.Width, Height = b.Height
            });
        }

        list.AddRange(GetWindows());
        return list;
    }

    private static IEnumerable<CaptureSource> GetWindows()
    {
        var windows = new List<CaptureSource>();
        EnumWindows((hWnd, _) =>
        {
            if (!IsWindowVisible(hWnd)) return true;
            int len = GetWindowTextLength(hWnd);
            if (len <= 0) return true;

            var sb = new StringBuilder(len + 1);
            GetWindowText(hWnd, sb, sb.Capacity);
            var title = sb.ToString().Trim();
            if (string.IsNullOrWhiteSpace(title)) return true;

            if (!GetWindowRect(hWnd, out var r)) return true;
            int w = r.Right - r.Left, h = r.Bottom - r.Top;
            if (w < 100 || h < 100) return true;

            windows.Add(new CaptureSource
            {
                Type = CaptureSourceType.Window,
                Name = $"Window: {title}",
                WindowTitle = title,
                Left = r.Left, Top = r.Top, Width = w, Height = h
            });
            return true;
        }, IntPtr.Zero);

        return windows.OrderBy(x => x.WindowTitle, StringComparer.OrdinalIgnoreCase).Take(150);
    }

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);
    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int maxCount);
    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hWnd);
    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }
}
