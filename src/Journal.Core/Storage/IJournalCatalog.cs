namespace Journal.Core.Storage;

public interface IJournalCatalog
{
    string RootPath { get; }

    IReadOnlyList<string> GetJournalNames();

    bool JournalExists(string journalName);

    /// <summary>Creates the journal folder structure and returns the sanitized journal name.</summary>
    string CreateJournal(string journalName);
}
