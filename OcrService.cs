using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace GameCursorTranslator;

internal sealed class OcrService
{
    private readonly OcrEngine _engine;

    public OcrService()
    {
        var language = new Windows.Globalization.Language("en-US");
        _engine = OcrEngine.TryCreateFromLanguage(language)
            ?? throw new InvalidOperationException(
                "未找到 Windows 英语 OCR 语言包。请在 Windows 设置 > 时间和语言 > 语言和区域中安装英语语言功能（基本键入/OCR）。");
    }

    // 返回鼠标所在的整行英文，而不是单个单词。
    public async Task<(string Text, Windows.Foundation.Rect Bounds)?> FindLineAtAsync(
        byte[] png, double cursorLocalX, double cursorLocalY)
    {
        using var stream = new InMemoryRandomAccessStream();

        var writer = new DataWriter(stream);
        writer.WriteBytes(png);
        await writer.StoreAsync();
        await writer.FlushAsync();
        writer.DetachStream();
        writer.Dispose();

        stream.Seek(0);
        var decoder = await BitmapDecoder.CreateAsync(stream);
        var bitmap = await decoder.GetSoftwareBitmapAsync(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied);

        try
        {
            var result = await _engine.RecognizeAsync(bitmap);

            var lines = result.Lines
                .Select(line =>
                {
                    var words = line.Words
                        .Where(w => !string.IsNullOrWhiteSpace(w.Text))
                        .OrderBy(w => w.BoundingRect.Left)
                        .ToList();

                    if (words.Count == 0)
                        return null;

                    double left = words.Min(w => w.BoundingRect.Left);
                    double top = words.Min(w => w.BoundingRect.Top);
                    double right = words.Max(w => w.BoundingRect.Right);
                    double bottom = words.Max(w => w.BoundingRect.Bottom);

                    string text = string.Join(" ",
                        words.Select(w => w.Text.Trim()).Where(t => t.Length > 0));

                    // 修正英文标点前多余的空格。
                    text = System.Text.RegularExpressions.Regex.Replace(
                        text, @"\s+([,.;:!?])", "$1");

                    var bounds = new Windows.Foundation.Rect(
                        left, top, right - left, bottom - top);

                    return new
                    {
                        Text = text,
                        Bounds = bounds,
                        Distance = DistanceToRect(cursorLocalX, cursorLocalY, bounds)
                    };
                })
                .Where(line => line != null && line.Text.Length > 0)
                .OrderBy(line => line!.Distance)
                .ToList();

            // 允许光标落在行内单词之间的空隙，或稍微偏离文字基线。
            var hit = lines.FirstOrDefault(line => line!.Distance <= 28);

            return hit == null
                ? null
                : (hit.Text, hit.Bounds);
        }
        finally
        {
            bitmap.Dispose();
        }
    }

    private static double DistanceToRect(
        double x, double y, Windows.Foundation.Rect r)
    {
        double dx = Math.Max(Math.Max(r.Left - x, 0), x - r.Right);
        double dy = Math.Max(Math.Max(r.Top - y, 0), y - r.Bottom);
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
