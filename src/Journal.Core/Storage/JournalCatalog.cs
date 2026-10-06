namespace Journal.Core.Storage;

public sealed class JournalCatalog : IJournalCatalog
{
    private readonly JournalLocations _locations;

    public JournalCatalog(JournalLocations locations)
    {
        _locations = locations;
    }

    public string RootPath => _locations.RootPath;

    public IReadOnlyList<string> GetJournalNames()
    {
        if (!Directory.Exists(_locations.RootPath))
        {
            return [];
        }

        return Directory.EnumerateDirectories(_locations.RootPath)
            .Select(folder => Path.GetFileName(folder))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
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
}
