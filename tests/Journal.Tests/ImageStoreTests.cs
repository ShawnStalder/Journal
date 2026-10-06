using Journal.Core;
using Journal.Core.Storage;
using Journal.Tests.TestSupport;

namespace Journal.Tests;

public sealed class ImageStoreTests
{
    [Theory]
    [InlineData("shot.png")]
    [InlineData("shot.BMP")]
    [InlineData("shot.jpg")]
    [InlineData("shot.jpeg")]
    public void AddImage_AcceptsSupportedFormats(string fileName)
    {
        using var root = new TemporaryJournalRoot();
        var journal = root.CreateJournal();
        var store = new ImageStore(root.Locations);

        var image = store.AddImage(journal, fileName, new MemoryStream([1, 2, 3]));

        Assert.True(File.Exists(image.FilePath));
        Assert.Equal(Path.Combine(root.Path, journal, "Images"), Path.GetDirectoryName(image.FilePath));
        Assert.Equal([1, 2, 3], File.ReadAllBytes(image.FilePath));
    }

    [Fact]
    public void AddImage_RejectsUnsupportedFormats()
    {
        using var root = new TemporaryJournalRoot();
        var journal = root.CreateJournal();
        var store = new ImageStore(root.Locations);

        Assert.Throws<JournalException>(() => store.AddImage(journal, "notes.gif", new MemoryStream([1])));
    }

    [Fact]
    public void AddImage_KeepsBothFilesWhenTheNameIsAlreadyUsed()
    {
        using var root = new TemporaryJournalRoot();
        var journal = root.CreateJournal();
        var store = new ImageStore(root.Locations);

        var first = store.AddImage(journal, "shot.png", new MemoryStream([1]));
        var second = store.AddImage(journal, "shot.png", new MemoryStream([2]));

        Assert.NotEqual(first.FilePath, second.FilePath);
        Assert.Equal(2, store.GetImages(journal).Count);
    }

    [Fact]
    public void GetImages_IgnoresFilesThatAreNotImages()
    {
        using var root = new TemporaryJournalRoot();
        var journal = root.CreateJournal();
        File.WriteAllText(Path.Combine(root.Locations.GetImagesFolder(journal), "Thumbs.db"), "x");
        var store = new ImageStore(root.Locations);
        store.AddImage(journal, "shot.png", new MemoryStream([1]));

        var image = Assert.Single(store.GetImages(journal));

        Assert.Equal("shot.png", image.FileName);
    }

    [Fact]
    public void DeleteImage_RemovesTheFile()
    {
        using var root = new TemporaryJournalRoot();
        var journal = root.CreateJournal();
        var store = new ImageStore(root.Locations);
        var image = store.AddImage(journal, "shot.png", new MemoryStream([1]));

        store.DeleteImage(image);

        Assert.False(File.Exists(image.FilePath));
    }
}
