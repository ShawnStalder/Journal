using Journal.Core.Storage;

namespace Journal.Tests.TestSupport;

public sealed class TemporaryJournalRoot : IDisposable
{
    public TemporaryJournalRoot()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"JournalTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
        Locations = new JournalLocations(Path);
        Clock = new FixedTimeProvider(new DateTime(2026, 10, 6, 14, 30, 0));
    }

    public string Path { get; }

    public JournalLocations Locations { get; }

    public FixedTimeProvider Clock { get; }

    public string CreateJournal(string name = "Sample")
    {
        return new JournalCatalog(Locations).CreateJournal(name);
    }

    public void Dispose()
    {
        Directory.Delete(Path, recursive: true);
    }
}
