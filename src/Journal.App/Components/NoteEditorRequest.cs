using Journal.Core.Models;

namespace Journal.App.Components;

/// <summary>Describes the note editor to show: a new note of the given type, or an existing note to edit.</summary>
public sealed record NoteEditorRequest(NoteType Type, JournalNote? Existing);
