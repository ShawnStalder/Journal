using Journal.Core.Models;

namespace Journal.Core.Storage;

public interface INoteStore
{
    /// <summary>Returns every note in the journal, newest first.</summary>
    IReadOnlyList<JournalNote> GetNotes(string journalName);

    JournalNote AddNote(string journalName, NoteType type, string title, string content);

    JournalNote UpdateNote(JournalNote note, string content);

    void DeleteNote(JournalNote note);
}
