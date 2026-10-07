using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using Journal.App.Services;
using Journal.Core.Rendering;
using Journal.Core.State;
using Journal.Core.Storage;
using Microsoft.Extensions.DependencyInjection;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace Journal.App;

public partial class App : System.Windows.Application
{
    private const string InstanceMutexName = @"Local\Journal.Tray";
    private static readonly TimeSpan RestartHandoffTimeout = TimeSpan.FromSeconds(8);

    private Mutex? _instanceMutex;
    private ServiceProvider? _services;
    private Forms.NotifyIcon? _trayIcon;
    private Drawing.Icon? _trayIconImage;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (!TryClaimSingleInstance())
        {
            Shutdown();
            return;
        }

        DispatcherUnhandledException += OnUnhandledException;

        _services = BuildServices();
        var theme = _services.GetRequiredService<ThemeService>();
        EnableAutoStartOnFirstRun();
        _trayIconImage = AppIcon.CreateTrayIcon();
        _trayIcon = new Forms.NotifyIcon
        {
            Icon = _trayIconImage,
            Text = "Journal",
            ContextMenuStrip = BuildTrayMenu(theme),
            Visible = true
        };
        _trayIcon.DoubleClick += (_, _) => _services.GetRequiredService<IWindowManager>().ShowOpenJournal();

        RestoreSession();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        _trayIconImage?.Dispose();
        _services?.Dispose();
        _instanceMutex?.ReleaseMutex();
        _instanceMutex?.Dispose();
        base.OnExit(e);
    }

    private static ServiceProvider BuildServices()
    {
        var stateFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Journal");

        var services = new ServiceCollection();
        services.AddWpfBlazorWebView();
#if DEBUG
        services.AddBlazorWebViewDeveloperTools();
#endif
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(new JournalLocations(GetRootPath()));
        services.AddSingleton<IJournalCatalog, JournalCatalog>();
        services.AddSingleton<INoteStore, NoteStore>();
        services.AddSingleton<ITaskStore, TaskStore>();
        services.AddSingleton<IImageStore, ImageStore>();
        services.AddSingleton<IJournalWatcherFactory, JournalWatcherFactory>();
        services.AddSingleton<NoteHtmlSanitizer>();
        services.AddSingleton(new SettingsStore(stateFolder));
        services.AddSingleton(provider => provider.GetRequiredService<SettingsStore>().Load());
        services.AddSingleton<AutoStartService>();
        services.AddSingleton(new SessionStore(stateFolder));
        services.AddSingleton<ThemeService>();
        services.AddSingleton<IThumbnailProvider, ThumbnailProvider>();
        services.AddSingleton<IWindowManager, WindowManager>();
        return services.BuildServiceProvider();
    }

    // Lets development and tests point at a local folder without touching the real share.
    private static string GetRootPath()
    {
        var overridePath = Environment.GetEnvironmentVariable("JOURNAL_ROOT_PATH");
        return string.IsNullOrWhiteSpace(overridePath)
            ? JournalLocations.DefaultRootPath
            : overridePath;
    }

    // A restart launches the new process before this one has exited, so give the old one time to let go.
    private bool TryClaimSingleInstance()
    {
        _instanceMutex = new Mutex(false, InstanceMutexName);
        try
        {
            return _instanceMutex.WaitOne(RestartHandoffTimeout);
        }
        catch (AbandonedMutexException)
        {
            return true;
        }
    }

    private Forms.ContextMenuStrip BuildTrayMenu(ThemeService theme)
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("New Journal", null, (_, _) => _services!.GetRequiredService<IWindowManager>().ShowNewJournal());
        menu.Items.Add("Open Journal", null, (_, _) => _services!.GetRequiredService<IWindowManager>().ShowOpenJournal());
        menu.Items.Add("Open Archived Journal", null, (_, _) => _services!.GetRequiredService<IWindowManager>().ShowOpenArchivedJournal());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(BuildThemeMenuItem(theme));
        menu.Items.Add(BuildAutoStartMenuItem());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Restart", null, (_, _) => Restart());
        menu.Items.Add("Exit", null, (_, _) => Shutdown());
        return menu;
    }

    private static Forms.ToolStripMenuItem BuildThemeMenuItem(ThemeService theme)
    {
        var themeMenu = new Forms.ToolStripMenuItem("Theme");
        var darkItem = new Forms.ToolStripMenuItem("Dark");
        var lightItem = new Forms.ToolStripMenuItem("Light");

        darkItem.Click += (_, _) => theme.SetDark(true);
        lightItem.Click += (_, _) => theme.SetDark(false);

        void RefreshChecks()
        {
            darkItem.Checked = theme.IsDark;
            lightItem.Checked = !theme.IsDark;
        }

        RefreshChecks();
        theme.Changed += RefreshChecks;

        themeMenu.DropDownItems.Add(darkItem);
        themeMenu.DropDownItems.Add(lightItem);
        return themeMenu;
    }

    // Opt-out rather than opt-in: a journal that is always in the tray is the intent, and the menu item undoes it.
    private void EnableAutoStartOnFirstRun()
    {
        var settings = _services!.GetRequiredService<AppSettings>();
        if (settings.HasConfiguredAutoStart)
        {
            return;
        }

        _services!.GetRequiredService<AutoStartService>().SetEnabled(true);
        settings.HasConfiguredAutoStart = true;
        _services!.GetRequiredService<SettingsStore>().Save(settings);
    }

    private Forms.ToolStripMenuItem BuildAutoStartMenuItem()
    {
        var autoStart = _services!.GetRequiredService<AutoStartService>();
        var item = new Forms.ToolStripMenuItem("Start with Windows") { Checked = autoStart.IsEnabled };
        item.Click += (_, _) =>
        {
            autoStart.SetEnabled(!autoStart.IsEnabled);
            item.Checked = autoStart.IsEnabled;
        };
        return item;
    }

    private void Restart()
    {
        var windows = _services!.GetRequiredService<IWindowManager>();
        var openJournals = windows.OpenJournalNames;
        if (openJournals.Count > 0)
        {
            _services!.GetRequiredService<SessionStore>().Save(openJournals);
        }

        Process.Start(Environment.ProcessPath!);
        Shutdown();
    }

    private void RestoreSession()
    {
        var windows = _services!.GetRequiredService<IWindowManager>();
        foreach (var journalName in _services!.GetRequiredService<SessionStore>().Take())
        {
            windows.ShowJournal(journalName);
        }
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Journal",
            "error.log");
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
        File.AppendAllText(logPath, $"{DateTime.Now:s} {e.Exception}{Environment.NewLine}{Environment.NewLine}");

        System.Windows.MessageBox.Show(
            $"Something went wrong: {e.Exception.Message}\n\nDetails were written to {logPath}",
            "Journal",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }
}
