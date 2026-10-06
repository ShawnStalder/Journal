namespace Journal.Core.Models;

public sealed record JournalNote(
    NoteType Type,
    string Title,
    DateTime CreatedOn,
    string FilePath,
    string Content)
{
    public string FileName => Path.GetFileName(FilePath);
}
