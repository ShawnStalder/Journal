namespace Journal.Core.Storage;

public interface IJournalWatcher : IDisposable
{
    /// <summary>Raised (on a background thread) after files in the journal change, once changes settle.</summary>
    event Action? Changed;
}

public interface IJournalWatcherFactory
{
    IJournalWatcher Watch(string journalName);
}

public sealed class JournalWatcherFactory : IJournalWatcherFactory
{
    private readonly JournalLocations _locations;

    public JournalWatcherFactory(JournalLocations locations)
    {
        _locations = locations;
    }

    public IJournalWatcher Watch(string journalName)
    {
        return new JournalWatcher(_locations.GetJournalFolder(journalName));
    }
}

public sealed class JournalWatcher : IJournalWatcher
{
    private static readonly TimeSpan SettleDelay = TimeSpan.FromMilliseconds(400);

    private readonly FileSystemWatcher _watcher;
    private readonly Timer _debounceTimer;

    public JournalWatcher(string journalFolder)
    {
        _debounceTimer = new Timer(_ => Changed?.Invoke(), null, Timeout.Infinite, Timeout.Infinite);

        _watcher = new FileSystemWatcher(journalFolder)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
        };
        _watcher.Created += OnFileSystemEvent;
        _watcher.Changed += OnFileSystemEvent;
        _watcher.Deleted += OnFileSystemEvent;
        _watcher.Renamed += OnFileSystemEvent;
        // A buffer overflow or dropped share means events were missed, so treat it as "something changed".
        _watcher.Error += (_, _) => ScheduleChanged();
        _watcher.EnableRaisingEvents = true;
    }

    public event Action? Changed;

    public void Dispose()
    {
        _watcher.Dispose();
        _debounceTimer.Dispose();
    }

    private void OnFileSystemEvent(object sender, FileSystemEventArgs e)
    {
        ScheduleChanged();
    }

    private void ScheduleChanged()
    {
        _debounceTimer.Change(SettleDelay, Timeout.InfiniteTimeSpan);
    }
}
