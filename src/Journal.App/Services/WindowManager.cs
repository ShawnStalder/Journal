using System.IO;
using System.Windows;
using Journal.App.Components;
using Journal.App.Windows;
using Journal.Core.Models;
using Journal.Core.Storage;

namespace Journal.App.Services;

public interface IWindowManager
{
    IReadOnlyCollection<string> OpenJournalNames { get; }

    void ShowNewJournal();

    void ShowOpenJournal();

    void ShowOpenArchivedJournal();

    /// <summary>Opens a journal by key: its name, or "Archive\name" for an archived journal.</summary>
    void ShowJournal(string journalName);

    void CloseJournal(string journalName);

    void ShowImage(JournalImage image);
}

public sealed class WindowManager : IWindowManager
{
    private readonly IServiceProvider _services;
    private readonly ThemeService _theme;
    private readonly Dictionary<string, BlazorWindow> _journalWindows = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ImageViewerWindow> _imageWindows = new(StringComparer.OrdinalIgnoreCase);
    private BlazorWindow? _newJournalWindow;
    private BlazorWindow? _openJournalWindow;
    private BlazorWindow? _openArchivedJournalWindow;

    public WindowManager(IServiceProvider services, ThemeService theme)
    {
        _services = services;
        _theme = theme;
    }

    public IReadOnlyCollection<string> OpenJournalNames => _journalWindows.Keys.ToList();

    public void ShowNewJournal()
    {
        if (TryActivate(_newJournalWindow))
        {
            return;
        }

        _newJournalWindow = CreateWindow(
            "New Journal",
            typeof(NewJournalPage),
            new Dictionary<string, object?>
            {
                [nameof(NewJournalPage.OnCreated)] = new Action<string>(name =>
                {
                    ShowJournal(name);
                    CloseLater(_newJournalWindow);
                }),
                [nameof(NewJournalPage.OnCancel)] = new Action(() => CloseLater(_newJournalWindow))
            },
            480,
            300);
        _newJournalWindow.Show();
    }

    public void ShowOpenJournal()
    {
        if (TryActivate(_openJournalWindow))
        {
            return;
        }

        _openJournalWindow = CreateWindow(
            "Open Journal",
            typeof(JournalListPage),
            new Dictionary<string, object?>
            {
                [nameof(JournalListPage.OnJournalChosen)] = new Action<string>(name =>
                {
                    ShowJournal(name);
                    CloseLater(_openJournalWindow);
                })
            },
            520,
            560);
        _openJournalWindow.Show();
    }

    public void ShowOpenArchivedJournal()
    {
        if (TryActivate(_openArchivedJournalWindow))
        {
            return;
        }

        _openArchivedJournalWindow = CreateWindow(
            "Open Archived Journal",
            typeof(JournalListPage),
            new Dictionary<string, object?>
            {
                [nameof(JournalListPage.ShowArchived)] = true,
                [nameof(JournalListPage.OnJournalChosen)] = new Action<string>(name =>
                {
                    ShowJournal(JournalLocations.GetArchivedKey(name));
                    CloseLater(_openArchivedJournalWindow);
                })
            },
            520,
            560);
        _openArchivedJournalWindow.Show();
    }

    public void CloseJournal(string journalName)
    {
        if (_journalWindows.TryGetValue(journalName, out var window))
        {
            CloseLater(window);
        }
    }

    public void ShowJournal(string journalName)
    {
        if (_journalWindows.TryGetValue(journalName, out var existing))
        {
            Activate(existing);
            return;
        }

        var archivedSuffix = JournalLocations.IsArchived(journalName) ? " (Archived)" : string.Empty;
        var window = CreateWindow(
            $"{JournalLocations.GetDisplayName(journalName)}{archivedSuffix} - Journal",
            typeof(JournalPage),
            new Dictionary<string, object?> { [nameof(JournalPage.JournalName)] = journalName },
            1320,
            840);
        window.Closed += (_, _) => _journalWindows.Remove(journalName);
        _journalWindows[journalName] = window;
        window.Show();
    }

    public void ShowImage(JournalImage image)
    {
        if (_imageWindows.TryGetValue(image.FilePath, out var existing))
        {
            Activate(existing);
            return;
        }

        try
        {
            var window = new ImageViewerWindow(image);
            window.Closed += (_, _) => _imageWindows.Remove(image.FilePath);
            _imageWindows[image.FilePath] = window;
            window.Show();
        }
        catch (NotSupportedException)
        {
            ShowImageError(image);
        }
        catch (IOException)
        {
            ShowImageError(image);
        }
        catch (UnauthorizedAccessException)
        {
            ShowImageError(image);
        }
    }

    private BlazorWindow CreateWindow(string title, Type pageType, IDictionary<string, object?> parameters, double width, double height)
    {
        return new BlazorWindow(_services, _theme, title, pageType, parameters, width, height);
    }

    private static bool TryActivate(Window? window)
    {
        if (window is not { IsLoaded: true })
        {
            return false;
        }

        Activate(window);
        return true;
    }

    private static void Activate(Window window)
    {
        if (window.WindowState == WindowState.Minimized)
        {
            window.WindowState = WindowState.Normal;
        }

        window.Activate();
    }

    // Closing from inside a Blazor event handler would tear down the page mid-dispatch.
    private static void CloseLater(Window? window)
    {
        window?.Dispatcher.BeginInvoke(window.Close);
    }

    private static void ShowImageError(JournalImage image)
    {
        System.Windows.MessageBox.Show(
            $"'{image.FileName}' could not be opened. It may be damaged or no longer available.",
            "Journal",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }
}
