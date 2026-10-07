using Journal.Core;
using Journal.Core.Models;
using Journal.Core.Storage;
using Journal.Tests.TestSupport;

namespace Journal.Tests;

public sealed class ArchiveTests
{
    [Fact]
    public void ArchiveJournal_MovesTheWholeJournalIntoTheArchiveFolder()
    {
        using var root = new TemporaryJournalRoot();
        var journal = root.CreateJournal("Old Project");
        new NoteStore(root.Locations, root.Clock).AddNote(journal, NoteType.Text, "Kept", "body");
        var catalog = new JournalCatalog(root.Locations);

        catalog.ArchiveJournal(journal);

        Assert.False(Directory.Exists(Path.Combine(root.Path, "Old Project")));
        Assert.True(Directory.Exists(Path.Combine(root.Path, "Archive", "Old Project", "Entries")));
        Assert.Single(Directory.GetFiles(Path.Combine(root.Path, "Archive", "Old Project", "Entries")));
    }

    [Fact]
    public void GetJournalNames_DoesNotListArchivedJournalsOrTheArchiveFolder()
    {
        using var root = new TemporaryJournalRoot();
        var catalog = new JournalCatalog(root.Locations);
        catalog.CreateJournal("Active");
        catalog.CreateJournal("Done");
        catalog.ArchiveJournal("Done");

        Assert.Equal(["Active"], catalog.GetJournalNames());
    }

    [Fact]
    public void GetArchivedJournalNames_ListsOnlyArchivedJournals()
    {
        using var root = new TemporaryJournalRoot();
        var catalog = new JournalCatalog(root.Locations);
        catalog.CreateJournal("Active");
        catalog.CreateJournal("Done");
        catalog.ArchiveJournal("Done");

        Assert.Equal(["Done"], catalog.GetArchivedJournalNames());
    }

    [Fact]
    public void GetArchivedJournalNames_ReturnsNothingBeforeAnythingIsArchived()
    {
        using var root = new TemporaryJournalRoot();

        Assert.Empty(new JournalCatalog(root.Locations).GetArchivedJournalNames());
    }

    [Fact]
    public void ArchiveJournal_RefusesToReplaceAnAlreadyArchivedJournal()
    {
        using var root = new TemporaryJournalRoot();
        var catalog = new JournalCatalog(root.Locations);
        catalog.CreateJournal("Same");
        catalog.ArchiveJournal("Same");
        catalog.CreateJournal("Same");

        Assert.Throws<JournalException>(() => catalog.ArchiveJournal("Same"));
        Assert.True(Directory.Exists(Path.Combine(root.Path, "Same")));
    }

    [Fact]
    public void ArchiveJournal_FailsForAJournalThatDoesNotExist()
    {
        using var root = new TemporaryJournalRoot();

        Assert.Throws<JournalException>(() => new JournalCatalog(root.Locations).ArchiveJournal("Missing"));
    }

    [Fact]
    public void RestoreJournal_MovesTheJournalBackToTheActiveList()
    {
        using var root = new TemporaryJournalRoot();
        var catalog = new JournalCatalog(root.Locations);
        catalog.CreateJournal("Back Again");
        catalog.ArchiveJournal("Back Again");

        catalog.RestoreJournal("Back Again");

        Assert.Equal(["Back Again"], catalog.GetJournalNames());
        Assert.Empty(catalog.GetArchivedJournalNames());
    }

    [Fact]
    public void CreateJournal_RejectsTheReservedArchiveName()
    {
        using var root = new TemporaryJournalRoot();

        Assert.Throws<JournalException>(() => new JournalCatalog(root.Locations).CreateJournal("archive"));
    }

    [Fact]
    public void StoresWorkOnAnArchivedJournalThroughItsKey()
    {
        using var root = new TemporaryJournalRoot();
        var catalog = new JournalCatalog(root.Locations);
        catalog.CreateJournal("Done");
        var tasks = new TaskStore(root.Locations, root.Clock);
        tasks.AddTask("Done", "Finish up");
        catalog.ArchiveJournal("Done");
        var key = JournalLocations.GetArchivedKey("Done");

        var task = Assert.Single(tasks.GetTasks(key));

        Assert.Equal("Finish up", task.Title);
        Assert.True(JournalLocations.IsArchived(key));
        Assert.Equal("Done", JournalLocations.GetDisplayName(key));
        Assert.False(JournalLocations.IsArchived("Done"));
    }
}
