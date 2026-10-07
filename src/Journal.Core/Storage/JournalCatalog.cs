namespace Journal.Core.Storage;

public sealed class JournalCatalog : IJournalCatalog
{
    private readonly JournalLocations _locations;

    public JournalCatalog(JournalLocations locations)
    {
        _locations = locations;
    }

    public string RootPath => _locations.RootPath;

    public string ArchivePath => _locations.ArchivePath;

    public IReadOnlyList<string> GetJournalNames()
    {
        return ListJournalFolders(_locations.RootPath)
            .Where(name => !IsArchiveFolder(name))
            .ToList();
    }

    public IReadOnlyList<string> GetArchivedJournalNames()
    {
        return ListJournalFolders(_locations.ArchivePath).ToList();
    }

    public void ArchiveJournal(string journalName)
    {
        var source = _locations.GetJournalFolder(journalName);
        var destination = Path.Combine(_locations.ArchivePath, journalName);
        MoveJournal(source, destination, $"An archived journal named '{journalName}' already exists.");
    }

    public void RestoreJournal(string journalName)
    {
        var source = Path.Combine(_locations.ArchivePath, journalName);
        var destination = _locations.GetJournalFolder(journalName);
        MoveJournal(source, destination, $"A journal named '{journalName}' already exists, so the archived one cannot be restored.");
    }

    public bool JournalExists(string journalName)
    {
        return Directory.Exists(_locations.GetJournalFolder(journalName));
    }

    public string CreateJournal(string journalName)
    {
        var name = FileNameSanitizer.Sanitize(journalName);
        if (name.Length == 0)
        {
            throw new JournalException("Enter a journal name.");
        }

        if (IsArchiveFolder(name))
        {
            throw new JournalException($"'{name}' is reserved for archived journals. Choose another name.");
        }

        if (JournalExists(name))
        {
            throw new JournalException($"A journal named '{name}' already exists.");
        }

        Directory.CreateDirectory(_locations.GetEntriesFolder(name));
        Directory.CreateDirectory(_locations.GetSqlFolder(name));
        Directory.CreateDirectory(_locations.GetImagesFolder(name));
        File.WriteAllText(_locations.GetTasksFile(name), TaskFileFormat.Serialize([]));

        return name;
    }

    private static bool IsArchiveFolder(string name)
    {
        return name.Equals(JournalLocations.ArchiveFolderName, StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> ListJournalFolders(string folder)
    {
        if (!Directory.Exists(folder))
        {
            return [];
        }

        return Directory.EnumerateDirectories(folder)
            .Select(path => Path.GetFileName(path))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void MoveJournal(string source, string destination, string alreadyExistsMessage)
    {
        if (!Directory.Exists(source))
        {
            throw new JournalException("That journal no longer exists. It may have been moved elsewhere.");
        }

        if (Directory.Exists(destination))
        {
            throw new JournalException(alreadyExistsMessage);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        Directory.Move(source, destination);
    }
}
