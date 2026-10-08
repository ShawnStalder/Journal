namespace Journal.Core.Storage;

public sealed class JournalLocations
{
    public const string DefaultRootPath = @"\\dbsfp1.dbs.local\users\sstalder\My Documents\Journals\";

    public const string TasksFileName = "JournalTasks.tsk";

    public const string EntriesFolderName = "Entries";

    public const string SqlFolderName = "SQL";

    public const string DiagramsFolderName = "Diagrams";

    public const string ImagesFolderName = "Images";

    public const string ArchiveFolderName = "Archive";

    public JournalLocations(string rootPath)
    {
        RootPath = rootPath;
    }

    public string RootPath { get; }

    public string ArchivePath => Path.Combine(RootPath, ArchiveFolderName);

    /// <summary>
    /// Journals are identified by their path relative to the root, so an archived journal is "Archive\Name" and
    /// every store, watcher and window works for it without special cases.
    /// </summary>
    public static string GetArchivedKey(string journalName) => Path.Combine(ArchiveFolderName, journalName);

    public static bool IsArchived(string journalKey)
    {
        return journalKey.StartsWith($"{ArchiveFolderName}{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);
    }

    public static string GetDisplayName(string journalKey) => Path.GetFileName(journalKey);

    public string GetJournalFolder(string journalName) => Path.Combine(RootPath, journalName);

    public string GetTasksFile(string journalName) => Path.Combine(GetJournalFolder(journalName), TasksFileName);

    public string GetEntriesFolder(string journalName) => Path.Combine(GetJournalFolder(journalName), EntriesFolderName);

    public string GetSqlFolder(string journalName) => Path.Combine(GetJournalFolder(journalName), SqlFolderName);

    public string GetDiagramsFolder(string journalName) => Path.Combine(GetJournalFolder(journalName), DiagramsFolderName);

    public string GetImagesFolder(string journalName) => Path.Combine(GetJournalFolder(journalName), ImagesFolderName);
}
