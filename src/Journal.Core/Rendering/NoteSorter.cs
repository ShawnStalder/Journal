using Journal.Core.Models;

namespace Journal.Core.Rendering;

public static class NoteSorter
{
    public static IReadOnlyList<JournalNote> Sort(IEnumerable<JournalNote> notes, NoteSortOrder order)
    {
        return order switch
        {
            NoteSortOrder.LastModified => notes
                .OrderByDescending(note => note.ModifiedOn)
                .ThenByDescending(note => note.CreatedOn)
                .ToList(),
            NoteSortOrder.Type => notes
                .OrderBy(note => note.Type)
                .ThenByDescending(note => note.CreatedOn)
                .ThenByDescending(note => note.FileName, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            _ => notes
                .OrderByDescending(note => note.CreatedOn)
                .ThenByDescending(note => note.FileName, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
    }
}
