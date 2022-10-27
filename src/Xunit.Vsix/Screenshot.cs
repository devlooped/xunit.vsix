using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace Xunit;

static class Screenshot
{
    public static string Capture()
    {
        var fileName = Guid.NewGuid().ToString("N") + ".jpg";
        var bitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
        var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(0, 0, 0, 0, bitmap.Size);
        bitmap.Save(fileName, ImageFormat.Jpeg);
        return fileName;
    }
}
