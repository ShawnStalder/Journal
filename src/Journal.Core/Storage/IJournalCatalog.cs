namespace Journal.Core.Storage;

public interface IJournalCatalog
{
    string RootPath { get; }

    string ArchivePath { get; }

    /// <summary>Returns the active journals; archived journals are not included.</summary>
    IReadOnlyList<string> GetJournalNames();

    IReadOnlyList<string> GetArchivedJournalNames();

    /// <summary>Moves the journal into the Archive folder. Close any file watcher on it first.</summary>
    void ArchiveJournal(string journalName);

    void RestoreJournal(string journalName);

    bool JournalExists(string journalName);

    /// <summary>Creates the journal folder structure and returns the sanitized journal name.</summary>
    string CreateJournal(string journalName);
}
