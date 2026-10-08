using Journal.Core.Models;

namespace Journal.Core.Storage;

public interface INoteStore
{
    /// <summary>Returns every note in the journal, newest first.</summary>
    IReadOnlyList<JournalNote> GetNotes(string journalName);

    JournalNote AddNote(string journalName, NoteType type, string title, string content);

    JournalNote UpdateNote(JournalNote note, string content);

    /// <summary>Renames the note's file, keeping its timestamp so it stays in the same place in the list.</summary>
    JournalNote RenameNote(JournalNote note, string title);

    void DeleteNote(JournalNote note);
}
