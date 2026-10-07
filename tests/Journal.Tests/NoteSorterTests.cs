using Journal.Core.Models;
using Journal.Core.Rendering;
using Journal.Core.Storage;
using Journal.Core.State;
using Journal.Tests.TestSupport;

namespace Journal.Tests;

public sealed class NoteSorterTests
{
    private static JournalNote Note(string title, NoteType type, DateTime createdOn, DateTime modifiedOn)
    {
        return new JournalNote(type, title, createdOn, modifiedOn, $@"C:\x\{title}.html", string.Empty);
    }

    private static readonly DateTime Day = new(2026, 10, 7, 9, 0, 0);

    [Fact]
    public void DateCreated_ListsNewestCreatedFirst()
    {
        var notes = new[]
        {
            Note("old", NoteType.Text, Day.AddHours(-2), Day.AddHours(5)),
            Note("new", NoteType.Sql, Day, Day),
            Note("mid", NoteType.Text, Day.AddHours(-1), Day.AddHours(-1))
        };

        var titles = NoteSorter.Sort(notes, NoteSortOrder.DateCreated)
            .Select(note => note.Title)
            .ToList();

        Assert.Equal(["new", "mid", "old"], titles);
    }

    [Fact]
    public void LastModified_ListsMostRecentlyEditedFirst()
    {
        var notes = new[]
        {
            Note("old but edited", NoteType.Text, Day.AddHours(-2), Day.AddHours(5)),
            Note("new", NoteType.Sql, Day, Day),
            Note("mid", NoteType.Text, Day.AddHours(-1), Day.AddHours(-1))
        };

        var titles = NoteSorter.Sort(notes, NoteSortOrder.LastModified)
            .Select(note => note.Title)
            .ToList();

        Assert.Equal(["old but edited", "new", "mid"], titles);
    }

    [Fact]
    public void Type_GroupsTextNotesBeforeSqlNotesNewestFirstWithinEach()
    {
        var notes = new[]
        {
            Note("sql new", NoteType.Sql, Day, Day),
            Note("text old", NoteType.Text, Day.AddHours(-3), Day.AddHours(-3)),
            Note("sql old", NoteType.Sql, Day.AddHours(-2), Day.AddHours(-2)),
            Note("text new", NoteType.Text, Day.AddHours(-1), Day.AddHours(-1))
        };

        var titles = NoteSorter.Sort(notes, NoteSortOrder.Type)
            .Select(note => note.Title)
            .ToList();

        Assert.Equal(["text new", "text old", "sql new", "sql old"], titles);
    }

    [Fact]
    public void NoteStore_ReportsWhenANoteWasLastModified()
    {
        using var root = new TemporaryJournalRoot();
        var journal = root.CreateJournal();
        var store = new NoteStore(root.Locations, root.Clock);
        var note = store.AddNote(journal, NoteType.Text, "Edited", "one");
        var earlier = new DateTime(2026, 1, 1, 8, 0, 0);
        File.SetLastWriteTime(note.FilePath, earlier);

        var reloaded = Assert.Single(store.GetNotes(journal));
        var updated = store.UpdateNote(reloaded, "two");

        Assert.Equal(earlier, reloaded.ModifiedOn);
        Assert.True(updated.ModifiedOn > earlier);
        Assert.Equal(reloaded.CreatedOn, updated.CreatedOn);
    }

    [Fact]
    public void Settings_RememberTheChosenSortOrder()
    {
        var folder = Path.Combine(Path.GetTempPath(), $"JournalSort_{Guid.NewGuid():N}");
        try
        {
            var store = new SettingsStore(folder);

            store.Save(new AppSettings { NoteSortOrder = NoteSortOrder.Type });

            Assert.Equal(NoteSortOrder.Type, store.Load().NoteSortOrder);
            Assert.Contains("\"Type\"", File.ReadAllText(Path.Combine(folder, "settings.json")));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void Settings_DefaultToNewestCreatedFirst()
    {
        Assert.Equal(NoteSortOrder.DateCreated, new AppSettings().NoteSortOrder);
    }
}
