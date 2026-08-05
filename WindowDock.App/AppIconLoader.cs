using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WindowDock.App;

internal static class AppIconLoader
{
    private const string PngFileName = "app-icon.png";

    public static ImageSource? LoadImageSource()
    {
        var path = GetAssetPath(PngFileName);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    public static Icon? LoadTrayIcon()
    {
        var icoPath = GetAssetPath("app-icon.ico");
        if (File.Exists(icoPath))
        {
            try
            {
                return new Icon(icoPath);
            }
            catch
            {
                // Fall through to PNG conversion.
            }
        }

        var pngPath = GetAssetPath(PngFileName);
        if (!File.Exists(pngPath))
        {
            return null;
        }

        try
        {
            using var bitmap = new Bitmap(pngPath);
            return Icon.FromHandle(bitmap.GetHicon());
        }
        catch
        {
            return null;
        }
    }

    private static string GetAssetPath(string fileName)
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(baseDir, "Assets", fileName);
    }
}
