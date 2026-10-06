using Journal.Core;
using Journal.Core.Storage;
using Journal.Tests.TestSupport;

namespace Journal.Tests;

public sealed class JournalCatalogTests
{
    [Fact]
    public void CreateJournal_BuildsTheExpectedFolderStructure()
    {
        using var root = new TemporaryJournalRoot();
        var catalog = new JournalCatalog(root.Locations);

        var name = catalog.CreateJournal("Sprint 42");

        Assert.Equal("Sprint 42", name);
        Assert.True(Directory.Exists(Path.Combine(root.Path, "Sprint 42", "Entries")));
        Assert.True(Directory.Exists(Path.Combine(root.Path, "Sprint 42", "SQL")));
        Assert.True(Directory.Exists(Path.Combine(root.Path, "Sprint 42", "Images")));
        Assert.True(File.Exists(Path.Combine(root.Path, "Sprint 42", "JournalTasks.tsk")));
    }

    [Fact]
    public void CreateJournal_RemovesCharactersThatAreInvalidInFileNames()
    {
        using var root = new TemporaryJournalRoot();
        var catalog = new JournalCatalog(root.Locations);

        var name = catalog.CreateJournal("Q3: Plans/Goals?");

        Assert.Equal("Q3 Plans Goals", name);
    }

    [Fact]
    public void CreateJournal_RejectsDuplicateNames()
    {
        using var root = new TemporaryJournalRoot();
        var catalog = new JournalCatalog(root.Locations);
        catalog.CreateJournal("Sample");

        Assert.Throws<JournalException>(() => catalog.CreateJournal("sample"));
    }

    [Fact]
    public void CreateJournal_RejectsBlankNames()
    {
        using var root = new TemporaryJournalRoot();
        var catalog = new JournalCatalog(root.Locations);

        Assert.Throws<JournalException>(() => catalog.CreateJournal("  ?? "));
    }

    [Fact]
    public void GetJournalNames_ReturnsSortedFolderNames()
    {
        using var root = new TemporaryJournalRoot();
        var catalog = new JournalCatalog(root.Locations);
        catalog.CreateJournal("Beta");
        catalog.CreateJournal("alpha");

        var names = catalog.GetJournalNames();

        Assert.Equal(["alpha", "Beta"], names);
    }

    [Fact]
    public void GetJournalNames_ReturnsNothingWhenTheRootDoesNotExist()
    {
        var catalog = new JournalCatalog(new JournalLocations(Path.Combine(Path.GetTempPath(), $"Missing_{Guid.NewGuid():N}")));

        Assert.Empty(catalog.GetJournalNames());
    }
}
