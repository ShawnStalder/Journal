using Journal.Core.Models;

namespace Journal.Core.Storage;

public interface IImageStore
{
    /// <summary>Returns every image in the journal, newest first.</summary>
    IReadOnlyList<JournalImage> GetImages(string journalName);

    JournalImage AddImage(string journalName, string fileName, Stream content);

    void DeleteImage(JournalImage image);
}
