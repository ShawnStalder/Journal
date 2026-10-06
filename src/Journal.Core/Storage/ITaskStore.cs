using Journal.Core.Models;

namespace Journal.Core.Storage;

public interface ITaskStore
{
    IReadOnlyList<JournalTask> GetTasks(string journalName);

    JournalTask AddTask(string journalName, string title);

    void SetTaskCompleted(string journalName, Guid taskId, bool isCompleted);

    void UpdateTaskTitle(string journalName, Guid taskId, string title);

    void DeleteTask(string journalName, Guid taskId);
}
