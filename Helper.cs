using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace BatteryTracker;

public static partial class Helper
{
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DestroyIcon(nint hIcon);

    public static Icon CreateTextIcon(string text, Font font, int canvasSize)
    {
        Bitmap bmp = new(canvasSize, canvasSize, PixelFormat.Format32bppArgb);

        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Transparent);

            SizeF szf = g.MeasureString(text, font);
            float x = (canvasSize - szf.Width) / 2;
            float y = (canvasSize - szf.Height) / 2;

            // g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            g.DrawString(text, font, Brushes.White, x, y);
        }

        IntPtr hIcon = bmp.GetHicon();
        Icon icon = (Icon)Icon.FromHandle(hIcon).Clone();

        DestroyIcon(hIcon);
        bmp.Dispose();

        return icon;
    }
}
