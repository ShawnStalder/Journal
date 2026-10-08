using Journal.Core;
using Journal.Core.Models;
using Journal.Core.Storage;
using Journal.Tests.TestSupport;

namespace Journal.Tests;

public sealed class NoteStoreTests
{
    [Fact]
    public void AddNote_SavesTextNotesAsHtmlInEntries()
    {
        using var root = new TemporaryJournalRoot();
        var journal = root.CreateJournal();
        var store = new NoteStore(root.Locations, root.Clock);

        var note = store.AddNote(journal, NoteType.Text, "Standup", "<b>Hello</b>");

        Assert.Equal(Path.Combine(root.Path, journal, "Entries", "2026-10-06_143000_Standup.html"), note.FilePath);
        Assert.Equal("<b>Hello</b>", File.ReadAllText(note.FilePath));
    }

    [Fact]
    public void AddNote_SavesSqlNotesWithTheSqlExtensionInSql()
    {
        using var root = new TemporaryJournalRoot();
        var journal = root.CreateJournal();
        var store = new NoteStore(root.Locations, root.Clock);

        var note = store.AddNote(journal, NoteType.Sql, "Find orders", "SELECT 1");

        Assert.Equal(Path.Combine(root.Path, journal, "SQL", "2026-10-06_143000_Find orders.sql"), note.FilePath);
        Assert.Equal("SELECT 1", File.ReadAllText(note.FilePath));
    }

    [Fact]
    public void AddNote_SavesDiagramNotesWithTheMmdExtensionInDiagrams()
    {
        using var root = new TemporaryJournalRoot();
        var journal = root.CreateJournal();
        var store = new NoteStore(root.Locations, root.Clock);

        var note = store.AddNote(journal, NoteType.Diagram, "Order flow", "flowchart TD\n    A --> B");

        Assert.Equal(Path.Combine(root.Path, journal, "Diagrams", "2026-10-06_143000_Order flow.mmd"), note.FilePath);
        Assert.Equal("flowchart TD\n    A --> B", File.ReadAllText(note.FilePath));
    }

    [Fact]
    public void AddNote_UsesADefaultTitleForDiagramsWhenNoneIsGiven()
    {
        using var root = new TemporaryJournalRoot();
        var journal = root.CreateJournal();
        var store = new NoteStore(root.Locations, root.Clock);

        var note = store.AddNote(journal, NoteType.Diagram, " ", "pie");

        Assert.Equal("Untitled diagram", note.Title);
    }

    [Fact]
    public void GetNotes_IncludesDiagramNotesAndToleratesAMissingDiagramsFolder()
    {
        using var root = new TemporaryJournalRoot();
        var journal = root.CreateJournal();
        Directory.Delete(root.Locations.GetDiagramsFolder(journal));
        var store = new NoteStore(root.Locations, root.Clock);
        Assert.Empty(store.GetNotes(journal));

        store.AddNote(journal, NoteType.Diagram, "Flow", "flowchart TD");

        var note = Assert.Single(store.GetNotes(journal));
        Assert.Equal(NoteType.Diagram, note.Type);
    }

    [Fact]
    public void GetNotes_LoadsDiagramFilesAddedByHandWithoutATimestamp()
    {
        using var root = new TemporaryJournalRoot();
        var journal = root.CreateJournal();
        File.WriteAllText(Path.Combine(root.Locations.GetDiagramsFolder(journal), "sketch.mmd"), "pie");
        var store = new NoteStore(root.Locations, root.Clock);

        var note = Assert.Single(store.GetNotes(journal));

        Assert.Equal("sketch", note.Title);
        Assert.Equal(NoteType.Diagram, note.Type);
    }

    [Fact]
    public void AddNote_UsesADefaultTitleWhenNoneIsGiven()
    {
        using var root = new TemporaryJournalRoot();
        var journal = root.CreateJournal();
        var store = new NoteStore(root.Locations, root.Clock);

        var note = store.AddNote(journal, NoteType.Text, "   ", "body");

        Assert.Equal("Untitled", note.Title);
    }

    [Fact]
    public void AddNote_DoesNotOverwriteANoteSavedInTheSameSecond()
    {
        using var root = new TemporaryJournalRoot();
        var journal = root.CreateJournal();
        var store = new NoteStore(root.Locations, root.Clock);

        var first = store.AddNote(journal, NoteType.Text, "Idea", "one");
        var second = store.AddNote(journal, NoteType.Text, "Idea", "two");

        Assert.NotEqual(first.FilePath, second.FilePath);
        Assert.Equal("one", File.ReadAllText(first.FilePath));
    }

    [Fact]
    public void GetNotes_ReturnsTextAndSqlNotesNewestFirst()
    {
        using var root = new TemporaryJournalRoot();
        var journal = root.CreateJournal();
        var store = new NoteStore(root.Locations, root.Clock);
        store.AddNote(journal, NoteType.Text, "Oldest", "a");
        root.Clock.Advance(TimeSpan.FromMinutes(5));
        store.AddNote(journal, NoteType.Sql, "Middle", "b");
        root.Clock.Advance(TimeSpan.FromMinutes(5));
        store.AddNote(journal, NoteType.Text, "Newest", "c");

        var titles = store.GetNotes(journal)
            .Select(note => note.Title)
            .ToList();

        Assert.Equal(["Newest", "Middle", "Oldest"], titles);
    }

    [Fact]
    public void GetNotes_ParsesTitleAndTimestampFromTheFileName()
    {
        using var root = new TemporaryJournalRoot();
        var journal = root.CreateJournal();
        var store = new NoteStore(root.Locations, root.Clock);
        store.AddNote(journal, NoteType.Text, "Design_review", "x");

        var note = Assert.Single(store.GetNotes(journal));

        Assert.Equal("Design_review", note.Title);
        Assert.Equal(new DateTime(2026, 10, 6, 14, 30, 0), note.CreatedOn);
    }

    [Fact]
    public void GetNotes_IncludesFilesThatDoNotFollowTheNamingPattern()
    {
        using var root = new TemporaryJournalRoot();
        var journal = root.CreateJournal();
        File.WriteAllText(Path.Combine(root.Locations.GetSqlFolder(journal), "adhoc.sql"), "SELECT 2");
        var store = new NoteStore(root.Locations, root.Clock);

        var note = Assert.Single(store.GetNotes(journal));

        Assert.Equal("adhoc", note.Title);
        Assert.Equal("SELECT 2", note.Content);
    }

    [Fact]
    public void UpdateNote_RewritesTheExistingFile()
    {
        using var root = new TemporaryJournalRoot();
        var journal = root.CreateJournal();
        var store = new NoteStore(root.Locations, root.Clock);
        var note = store.AddNote(journal, NoteType.Sql, "Query", "SELECT 1");

        var updated = store.UpdateNote(note, "SELECT 2");

        Assert.Equal("SELECT 2", File.ReadAllText(note.FilePath));
        Assert.Equal("SELECT 2", updated.Content);
    }

    [Fact]
    public void UpdateNote_FailsWhenTheFileWasRemoved()
    {
        using var root = new TemporaryJournalRoot();
        var journal = root.CreateJournal();
        var store = new NoteStore(root.Locations, root.Clock);
        var note = store.AddNote(journal, NoteType.Text, "Gone", "x");
        File.Delete(note.FilePath);

        Assert.Throws<JournalException>(() => store.UpdateNote(note, "y"));
    }

    [Fact]
    public void DeleteNote_RemovesTheFile()
    {
        using var root = new TemporaryJournalRoot();
        var journal = root.CreateJournal();
        var store = new NoteStore(root.Locations, root.Clock);
        var note = store.AddNote(journal, NoteType.Text, "Temp", "x");

        store.DeleteNote(note);

        Assert.False(File.Exists(note.FilePath));
    }
}
