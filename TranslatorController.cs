using System.Windows;

namespace GameCursorTranslator;

internal sealed class TranslatorController : IDisposable
{
    private readonly OverlayWindow _overlay = new();
    private readonly TranslationService _translator = new();
    private OcrService? _ocr;
    private CancellationTokenSource? _cts;
    private Task? _worker;
    private bool _paused;
    private string _lastLine = "";
    private DateTime _lastAttempt = DateTime.MinValue;

    public bool IsRunning => _cts is not null;
    public event Action<string>? StatusChanged;

    public async Task StartAsync()
    {
        if (IsRunning) return;
        _ocr = new OcrService();
        _cts = new CancellationTokenSource();
        _paused = false;
        _lastLine = "";
        StatusChanged?.Invoke("正在运行：将鼠标停在英文句子所在行");
        _worker = Task.Run(() => RunLoopAsync(_cts.Token));
        await Task.CompletedTask;
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _lastLine = "";
        _overlay.Dispatcher.Invoke(_overlay.HideTranslation);
        StatusChanged?.Invoke("已停止");
    }

    public void TogglePause()
    {
        if (!IsRunning) return;
        _paused = !_paused;
        if (_paused) _overlay.Dispatcher.Invoke(_overlay.HideTranslation);
        StatusChanged?.Invoke(_paused ? "已暂停" : "正在运行：将鼠标停在英文句子所在行");
    }

    private async Task RunLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                if (_paused)
                {
                    await Task.Delay(200, token);
                    continue;
                }

                NativeMethods.GetCursorPos(out var p);
                if (DateTime.UtcNow - _lastAttempt < TimeSpan.FromMilliseconds(300))
                {
                    await Task.Delay(80, token);
                    continue;
                }
                _lastAttempt = DateTime.UtcNow;

                const int width = 560, height = 220;
                int cropLeft = Math.Max(0, p.X - width / 2);
                int cropTop = Math.Max(0, p.Y - height / 2);
                var png = ScreenCapture.CapturePng(p.X, p.Y, width, height);
                var hit = await _ocr!.FindLineAtAsync(
                    png, p.X - cropLeft, p.Y - cropTop);

                if (hit is null)
                {
                    if (_lastLine.Length > 0)
                    {
                        _lastLine = "";
                        _overlay.Dispatcher.Invoke(_overlay.HideTranslation);
                    }
                }
                else
                {
                    string line = hit.Value.Text.Trim();
                    if (line.Length > 0 &&
                        !string.Equals(line, _lastLine, StringComparison.OrdinalIgnoreCase))
                    {
                        _lastLine = line;
                        try
                        {
                            string translation = await _translator.TranslateAsync(line, token);
                            NativeMethods.GetCursorPos(out var now);
                            _overlay.Dispatcher.Invoke(() =>
                                _overlay.ShowTranslation(line, translation, now.X, now.Y));
                        }
                        catch (OperationCanceledException) { }
                        catch
                        {
                            _overlay.Dispatcher.Invoke(() =>
                                _overlay.ShowTranslation(line, "翻译暂不可用", p.X, p.Y));
                        }
                    }
                }

                await Task.Delay(160, token);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                StatusChanged?.Invoke("识别异常：" + ex.Message);
                await Task.Delay(700, token);
            }
        }
    }

    public void Dispose()
    {
        Stop();
        _overlay.Close();
    }
}
