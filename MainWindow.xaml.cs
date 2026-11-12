using System.Windows;
using System.Windows.Input;

namespace GameCursorTranslator;

public partial class MainWindow : Window
{
    private readonly TranslatorController _controller = new();
    public MainWindow()
    {
        InitializeComponent();
        _controller.StatusChanged += s => Dispatcher.Invoke(() => StatusText.Text = "状态：" + s);
    }

    private async void Start_Click(object sender, RoutedEventArgs e)
    {
        if (_controller.IsRunning) { _controller.Stop(); StartButton.Content = "开始取词  (F8)"; return; }
        try
        {
            await _controller.StartAsync();
            StartButton.Content = "停止取词  (F8)";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "启动失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Pause_Click(object sender, RoutedEventArgs e) => _controller.TogglePause();

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.F8)
        {
            if (_controller.IsRunning) { _controller.Stop(); StartButton.Content = "开始取词  (F8)"; }
            else _ = StartFromHotkeyAsync();
            e.Handled = true;
        }
        else if (e.Key == Key.F9) { _controller.TogglePause(); e.Handled = true; }
        else if (e.Key == Key.Escape) Close();
        base.OnKeyDown(e);
    }

    private async Task StartFromHotkeyAsync()
    {
        try { await _controller.StartAsync(); StartButton.Content = "停止取词  (F8)"; }
        catch (Exception ex) { MessageBox.Show(ex.Message, "启动失败"); }
    }

    protected override void OnClosed(EventArgs e)
    {
        _controller.Dispose();
        base.OnClosed(e);
    }
}
