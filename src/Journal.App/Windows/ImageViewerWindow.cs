using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Journal.App.Services;
using Journal.Core.Models;

namespace Journal.App.Windows;

/// <summary>Shows one image, fitted to the window. Click the image (or press Space) to toggle actual size.</summary>
public sealed class ImageViewerWindow : Window
{
    private readonly Image _image;
    private readonly ScrollViewer _scrollViewer;
    private bool _isFitted = true;

    public ImageViewerWindow(JournalImage journalImage)
    {
        Title = journalImage.FileName;
        Width = 1000;
        Height = 750;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new SolidColorBrush(Color.FromRgb(0x18, 0x18, 0x1a));
        Icon = AppIcon.CreateWindowIcon();

        _image = new Image
        {
            Source = LoadBitmap(journalImage.FilePath),
            Cursor = Cursors.Hand,
            ToolTip = "Click to toggle between fit and actual size"
        };
        _scrollViewer = new ScrollViewer { Content = _image };
        Content = _scrollViewer;

        ShowFitted();
        _image.MouseLeftButtonUp += (_, _) => ToggleSize();
        KeyDown += OnKeyDown;
        SourceInitialized += (_, _) => TitleBarTheme.Apply(this, isDark: true);
    }

    private static BitmapImage LoadBitmap(string path)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.UriSource = new Uri(path);
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
        }
        else if (e.Key == Key.Space)
        {
            ToggleSize();
        }
    }

    private void ToggleSize()
    {
        _isFitted = !_isFitted;
        if (_isFitted)
        {
            ShowFitted();
        }
        else
        {
            ShowActualSize();
        }
    }

    private void ShowFitted()
    {
        _image.Stretch = Stretch.Uniform;
        _image.StretchDirection = StretchDirection.DownOnly;
        _scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        _scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
    }

    private void ShowActualSize()
    {
        _image.Stretch = Stretch.None;
        _scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
        _scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
    }
}
