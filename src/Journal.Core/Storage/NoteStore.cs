using System.Globalization;
using System.Text.RegularExpressions;
using Journal.Core.Models;

namespace Journal.Core.Storage;

/// <summary>
/// Stores text notes as .html files in Entries and SQL notes as .sql files in SQL.
/// File names are "yyyy-MM-dd_HHmmss_Title.ext" so they sort chronologically on disk.
/// </summary>
public sealed partial class NoteStore : INoteStore
{
    private const string TimestampFormat = "yyyy-MM-dd_HHmmss";
    private const string TextExtension = ".html";
    private const string SqlExtension = ".sql";
    private const string DefaultTextTitle = "Untitled";
    private const string DefaultSqlTitle = "Untitled query";

    private readonly JournalLocations _locations;
    private readonly TimeProvider _timeProvider;

    public NoteStore(JournalLocations locations, TimeProvider timeProvider)
    {
        _locations = locations;
        _timeProvider = timeProvider;
    }

    public IReadOnlyList<JournalNote> GetNotes(string journalName)
    {
        var textNotes = ReadNotes(_locations.GetEntriesFolder(journalName), NoteType.Text, TextExtension);
        var sqlNotes = ReadNotes(_locations.GetSqlFolder(journalName), NoteType.Sql, SqlExtension);

        return textNotes
            .Concat(sqlNotes)
            .OrderByDescending(note => note.CreatedOn)
            .ThenByDescending(note => note.FileName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public JournalNote AddNote(string journalName, NoteType type, string title, string content)
    {
        var folder = type == NoteType.Sql
            ? _locations.GetSqlFolder(journalName)
            : _locations.GetEntriesFolder(journalName);
        var extension = type == NoteType.Sql ? SqlExtension : TextExtension;
        var defaultTitle = type == NoteType.Sql ? DefaultSqlTitle : DefaultTextTitle;

        var cleanTitle = FileNameSanitizer.Sanitize(title);
        if (cleanTitle.Length == 0)
        {
            cleanTitle = defaultTitle;
        }

        var createdOn = _timeProvider.GetLocalNow().DateTime;
        var baseName = $"{createdOn.ToString(TimestampFormat, CultureInfo.InvariantCulture)}_{cleanTitle}";

        Directory.CreateDirectory(folder);
        var path = FileNameSanitizer.GetUniquePath(folder, baseName, extension);
        File.WriteAllText(path, content);

        return new JournalNote(type, cleanTitle, ParseCreatedOn(Path.GetFileNameWithoutExtension(path), path), path, content);
    }

    public JournalNote UpdateNote(JournalNote note, string content)
    {
        if (!File.Exists(note.FilePath))
        {
            throw new JournalException("That note no longer exists. It may have been removed elsewhere.");
        }

        File.WriteAllText(note.FilePath, content);
        return note with { Content = content };
    }

    public void DeleteNote(JournalNote note)
    {
        File.Delete(note.FilePath);
    }

    private static IEnumerable<JournalNote> ReadNotes(string folder, NoteType type, string extension)
    {
        if (!Directory.Exists(folder))
        {
            return [];
        }

        return Directory.EnumerateFiles(folder, $"*{extension}")
            .Select(path => ReadNote(path, type))
            .ToList();
    }

    private static JournalNote ReadNote(string path, NoteType type)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        var match = NameParts().Match(name);
        var title = match.Success ? match.Groups["title"].Value : name;

        return new JournalNote(type, title, ParseCreatedOn(name, path), path, File.ReadAllText(path));
    }

    // Files dropped in by hand may not follow the naming pattern, so fall back to the file's own timestamp.
    private static DateTime ParseCreatedOn(string name, string path)
    {
        var match = NameParts().Match(name);
        var stamp = match.Success ? match.Groups["stamp"].Value : string.Empty;

        return DateTime.TryParseExact(stamp, TimestampFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var createdOn)
            ? createdOn
            : File.GetLastWriteTime(path);
    }

    [GeneratedRegex(@"^(?<stamp>\d{4}-\d{2}-\d{2}_\d{6})_(?<title>.*)$")]
    private static partial Regex NameParts();
}
