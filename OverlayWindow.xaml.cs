using System.Windows;
using System.Windows.Interop;

namespace GameCursorTranslator;

public partial class OverlayWindow : Window
{
    public OverlayWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            int ex = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
            NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE,
                ex | NativeMethods.WS_EX_TRANSPARENT |
                NativeMethods.WS_EX_TOOLWINDOW |
                NativeMethods.WS_EX_NOACTIVATE);
        };
    }

    public void ShowTranslation(string original, string translated, int cursorX, int cursorY)
    {
        OriginalText.Text = original;
        TranslationText.Text = translated;

        Width = Math.Clamp(Math.Max(translated.Length * 22 + 36,
            Math.Min(original.Length * 7 + 36, 520)), 260, 620);

        if (!IsVisible)
            Show();

        UpdateLayout();

        uint dpiX = 96, dpiY = 96;
        try
        {
            var point = new NativeMethods.POINT { X = cursorX, Y = cursorY };
            var monitor = NativeMethods.MonitorFromPoint(point, 2); // 最近的显示器
            if (monitor != IntPtr.Zero)
                NativeMethods.GetDpiForMonitor(monitor, 0, out dpiX, out dpiY);
        }
        catch
        {
            dpiX = dpiY = 96;
        }

        if (dpiX == 0) dpiX = 96;
        if (dpiY == 0) dpiY = 96;

        double scaleX = dpiX / 96.0;
        double scaleY = dpiY / 96.0;

        // 5 mm 转换为 WPF DIP；换算后仍对应显示器上的真实 5 mm。
        double gapDip = (5.0 * dpiY / 25.4) / scaleY;
        double cursorDipX = cursorX / scaleX;
        double cursorDipY = cursorY / scaleY;

        Left = cursorDipX - ActualWidth / 2;
        Top = cursorDipY - ActualHeight - gapDip;
    }

    public void HideTranslation()
    {
        if (IsVisible) Hide();
    }
}
