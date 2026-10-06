using Journal.Core.Models;

namespace Journal.Core.Storage;

public sealed class TaskStore : ITaskStore
{
    private readonly JournalLocations _locations;
    private readonly TimeProvider _timeProvider;

    public TaskStore(JournalLocations locations, TimeProvider timeProvider)
    {
        _locations = locations;
        _timeProvider = timeProvider;
    }

    public IReadOnlyList<JournalTask> GetTasks(string journalName)
    {
        var tasks = Load(journalName);

        var openTasks = tasks
            .Where(task => !task.IsCompleted)
            .OrderBy(task => task.CreatedOn);

        var completedTasks = tasks
            .Where(task => task.IsCompleted)
            .OrderByDescending(task => task.CompletedOn);

        return openTasks.Concat(completedTasks).ToList();
    }

    public JournalTask AddTask(string journalName, string title)
    {
        var task = new JournalTask
        {
            Id = Guid.NewGuid(),
            Title = RequireTitle(title),
            CreatedOn = _timeProvider.GetLocalNow().DateTime
        };

        Modify(journalName, tasks => tasks.Add(task));
        return task;
    }

    public void SetTaskCompleted(string journalName, Guid taskId, bool isCompleted)
    {
        Modify(journalName, tasks =>
        {
            var task = FindTask(tasks, taskId);
            task.IsCompleted = isCompleted;
            task.CompletedOn = isCompleted ? _timeProvider.GetLocalNow().DateTime : null;
        });
    }

    public void UpdateTaskTitle(string journalName, Guid taskId, string title)
    {
        var trimmedTitle = RequireTitle(title);
        Modify(journalName, tasks => FindTask(tasks, taskId).Title = trimmedTitle);
    }

    public void DeleteTask(string journalName, Guid taskId)
    {
        Modify(journalName, tasks => tasks.Remove(FindTask(tasks, taskId)));
    }

    private static string RequireTitle(string title)
    {
        var trimmedTitle = title.Trim();
        if (trimmedTitle.Length == 0)
        {
            throw new JournalException("Enter a task description.");
        }

        return trimmedTitle;
    }

    private static JournalTask FindTask(List<JournalTask> tasks, Guid taskId)
    {
        return tasks.FirstOrDefault(task => task.Id == taskId)
            ?? throw new JournalException("That task no longer exists. It may have been removed elsewhere.");
    }

    private List<JournalTask> Load(string journalName)
    {
        var path = _locations.GetTasksFile(journalName);
        return File.Exists(path)
            ? TaskFileFormat.Deserialize(File.ReadAllText(path))
            : [];
    }

    // Re-reading before every write keeps edits made to the file by another process.
    private void Modify(string journalName, Action<List<JournalTask>> change)
    {
        var tasks = Load(journalName);
        change(tasks);

        var path = _locations.GetTasksFile(journalName);
        var temporaryPath = $"{path}.tmp";
        File.WriteAllText(temporaryPath, TaskFileFormat.Serialize(tasks));
        File.Move(temporaryPath, path, overwrite: true);
    }
}
