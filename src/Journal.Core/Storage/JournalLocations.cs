namespace Journal.Core.Storage;

public sealed class JournalLocations
{
    public const string DefaultRootPath = @"\\dbsfp1.dbs.local\users\sstalder\My Documents\Journals\";

    public const string TasksFileName = "JournalTasks.tsk";

    public const string EntriesFolderName = "Entries";

    public const string SqlFolderName = "SQL";

    public const string ImagesFolderName = "Images";

    public JournalLocations(string rootPath)
    {
        RootPath = rootPath;
    }

    public string RootPath { get; }

    public string GetJournalFolder(string journalName) => Path.Combine(RootPath, journalName);

    public string GetTasksFile(string journalName) => Path.Combine(GetJournalFolder(journalName), TasksFileName);

    public string GetEntriesFolder(string journalName) => Path.Combine(GetJournalFolder(journalName), EntriesFolderName);

    public string GetSqlFolder(string journalName) => Path.Combine(GetJournalFolder(journalName), SqlFolderName);

    public string GetImagesFolder(string journalName) => Path.Combine(GetJournalFolder(journalName), ImagesFolderName);
}
