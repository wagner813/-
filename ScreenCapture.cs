using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
namespace GameCursorTranslator;

internal static class ScreenCapture
{
    public static byte[] CapturePng(int x, int y, int width, int height)
    {
        int left = Math.Max(0, x - width / 2);
        int top = Math.Max(0, y - height / 2);
        using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        IntPtr screen = NativeMethods.GetDC(IntPtr.Zero);
        IntPtr memory = NativeMethods.CreateCompatibleDC(screen);
        IntPtr hBitmap = bitmap.GetHbitmap();
        IntPtr old = NativeMethods.SelectObject(memory, hBitmap);
        try
        {
            if (!NativeMethods.BitBlt(memory, 0, 0, width, height, screen, left, top, NativeMethods.SRCCOPY))
                throw new InvalidOperationException("无法截取屏幕区域。请检查游戏是否使用独占全屏或受保护画面。");
            using var ms = new MemoryStream();
            using (var g = Graphics.FromImage(bitmap))
            {
                // BitBlt writes to the selected HBITMAP; the managed bitmap is populated below through GDI+ clone.
            }
            using var captured = Image.FromHbitmap(hBitmap);
            captured.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }
        finally
        {
            NativeMethods.SelectObject(memory, old);
            NativeMethods.DeleteObject(hBitmap);
            NativeMethods.DeleteDC(memory);
            NativeMethods.ReleaseDC(IntPtr.Zero, screen);
        }
    }
}
