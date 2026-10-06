using Journal.Core;
using Journal.Core.Storage;
using Journal.Tests.TestSupport;

namespace Journal.Tests;

public sealed class TaskStoreTests
{
    [Fact]
    public void AddTask_PersistsTheTaskAsOpen()
    {
        using var root = new TemporaryJournalRoot();
        var journal = root.CreateJournal();
        var store = new TaskStore(root.Locations, root.Clock);

        store.AddTask(journal, "  Write report  ");

        var task = Assert.Single(store.GetTasks(journal));
        Assert.Equal("Write report", task.Title);
        Assert.False(task.IsCompleted);
        Assert.Null(task.CompletedOn);
        Assert.Equal(new DateTime(2026, 10, 6, 14, 30, 0), task.CreatedOn);
    }

    [Fact]
    public void AddTask_RejectsBlankTitles()
    {
        using var root = new TemporaryJournalRoot();
        var journal = root.CreateJournal();
        var store = new TaskStore(root.Locations, root.Clock);

        Assert.Throws<JournalException>(() => store.AddTask(journal, "   "));
    }

    [Fact]
    public void SetTaskCompleted_RecordsWhenTheTaskWasCompleted()
    {
        using var root = new TemporaryJournalRoot();
        var journal = root.CreateJournal();
        var store = new TaskStore(root.Locations, root.Clock);
        var task = store.AddTask(journal, "Ship it");
        root.Clock.Advance(TimeSpan.FromHours(2));

        store.SetTaskCompleted(journal, task.Id, isCompleted: true);

        var saved = Assert.Single(store.GetTasks(journal));
        Assert.True(saved.IsCompleted);
        Assert.Equal(new DateTime(2026, 10, 6, 16, 30, 0), saved.CompletedOn);
    }

    [Fact]
    public void SetTaskCompleted_ClearsTheCompletionTimeWhenReopened()
    {
        using var root = new TemporaryJournalRoot();
        var journal = root.CreateJournal();
        var store = new TaskStore(root.Locations, root.Clock);
        var task = store.AddTask(journal, "Ship it");
        store.SetTaskCompleted(journal, task.Id, isCompleted: true);

        store.SetTaskCompleted(journal, task.Id, isCompleted: false);

        var saved = Assert.Single(store.GetTasks(journal));
        Assert.False(saved.IsCompleted);
        Assert.Null(saved.CompletedOn);
    }

    [Fact]
    public void GetTasks_ListsOpenTasksBeforeCompletedOnes()
    {
        using var root = new TemporaryJournalRoot();
        var journal = root.CreateJournal();
        var store = new TaskStore(root.Locations, root.Clock);
        var first = store.AddTask(journal, "First");
        root.Clock.Advance(TimeSpan.FromMinutes(1));
        store.AddTask(journal, "Second");
        store.SetTaskCompleted(journal, first.Id, isCompleted: true);

        var titles = store.GetTasks(journal)
            .Select(task => task.Title)
            .ToList();

        Assert.Equal(["Second", "First"], titles);
    }

    [Fact]
    public void Changes_KeepTasksAddedToTheFileByAnotherProcess()
    {
        using var root = new TemporaryJournalRoot();
        var journal = root.CreateJournal();
        var store = new TaskStore(root.Locations, root.Clock);
        store.AddTask(journal, "Mine");
        var path = root.Locations.GetTasksFile(journal);
        var externalTasks = TaskFileFormat.Deserialize(File.ReadAllText(path));
        externalTasks.Add(new Journal.Core.Models.JournalTask { Id = Guid.NewGuid(), Title = "External" });
        File.WriteAllText(path, TaskFileFormat.Serialize(externalTasks));

        store.AddTask(journal, "Another of mine");

        Assert.Equal(3, store.GetTasks(journal).Count);
    }

    [Fact]
    public void DeleteTask_RemovesTheTask()
    {
        using var root = new TemporaryJournalRoot();
        var journal = root.CreateJournal();
        var store = new TaskStore(root.Locations, root.Clock);
        var task = store.AddTask(journal, "Remove me");

        store.DeleteTask(journal, task.Id);

        Assert.Empty(store.GetTasks(journal));
    }

    [Fact]
    public void SetTaskCompleted_FailsForAnUnknownTask()
    {
        using var root = new TemporaryJournalRoot();
        var journal = root.CreateJournal();
        var store = new TaskStore(root.Locations, root.Clock);

        Assert.Throws<JournalException>(() => store.SetTaskCompleted(journal, Guid.NewGuid(), isCompleted: true));
    }

    [Fact]
    public void Deserialize_ReportsMalformedFilesAsJournalErrors()
    {
        Assert.Throws<JournalException>(() => TaskFileFormat.Deserialize("{ not json"));
    }
}
