using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Drawing = System.Drawing;

namespace Journal.App.Services;

public static class AppIcon
{
    private static readonly Uri IconUri = new("pack://application:,,,/Assets/AppIcon.ico");

    public static ImageSource CreateWindowIcon()
    {
        return BitmapFrame.Create(IconUri);
    }

    // The tray needs a System.Drawing.Icon rather than a WPF ImageSource.
    public static Drawing.Icon CreateTrayIcon()
    {
        var resource = System.Windows.Application.GetResourceStream(IconUri)
            ?? throw new InvalidOperationException("Assets/AppIcon.ico resource not found.");
        using var stream = resource.Stream;
        return new Drawing.Icon(stream);
    }
}
