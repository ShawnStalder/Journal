using System.Windows;
using System.Windows.Media;
using Journal.App.Components;
using Journal.App.Services;
using Microsoft.AspNetCore.Components.WebView.Wpf;
using Microsoft.Web.WebView2.Wpf;

namespace Journal.App.Windows;

/// <summary>A WPF window whose entire content is a Blazor page.</summary>
public sealed class BlazorWindow : Window
{
    private static readonly Color DarkBackground = Color.FromRgb(0x1e, 0x1f, 0x22);
    private static readonly Color LightBackground = Color.FromRgb(0xf4, 0xf5, 0xf7);

    private readonly ThemeService _theme;
    private WebView2CompositionControl? _browser;

    public BlazorWindow(
        IServiceProvider services,
        ThemeService theme,
        string title,
        Type pageType,
        IDictionary<string, object?> pageParameters,
        double width,
        double height)
    {
        _theme = theme;

        var workArea = SystemParameters.WorkArea;
        Title = title;
        Width = Math.Min(width, workArea.Width * 0.92);
        Height = Math.Min(height, workArea.Height * 0.92);
        MinWidth = Math.Min(width, 480);
        MinHeight = Math.Min(height, 320);
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Icon = AppIcon.CreateWindowIcon();

        var webView = new BlazorWebView
        {
            HostPage = @"wwwroot\index.html",
            Services = services
        };
        webView.RootComponents.Add(new RootComponent
        {
            Selector = "#app",
            ComponentType = typeof(AppShell),
            Parameters = new Dictionary<string, object?>
            {
                [nameof(AppShell.PageType)] = pageType,
                [nameof(AppShell.PageParameters)] = pageParameters
            }
        });
        webView.BlazorWebViewInitialized += (_, e) =>
        {
            _browser = e.WebView;
            _browser.DefaultBackgroundColor = GetDrawingBackground();
            _browser.NavigationCompleted += (_, _) => FocusBrowser();
        };
        Content = webView;

        // The page's autofocus only takes effect once the embedded browser itself holds keyboard focus.
        Activated += (_, _) => FocusBrowser();

        ApplyTheme();
        _theme.Changed += ApplyTheme;
        SourceInitialized += (_, _) => TitleBarTheme.Apply(this, _theme.IsDark);
        Closed += (_, _) => _theme.Changed -= ApplyTheme;
    }

    private void FocusBrowser()
    {
        _browser?.Focus();
    }

    private void ApplyTheme()
    {
        var color = _theme.IsDark ? DarkBackground : LightBackground;
        Background = new SolidColorBrush(color);
        TitleBarTheme.Apply(this, _theme.IsDark);
    }

    private System.Drawing.Color GetDrawingBackground()
    {
        var color = _theme.IsDark ? DarkBackground : LightBackground;
        return System.Drawing.Color.FromArgb(color.R, color.G, color.B);
    }
}
