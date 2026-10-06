using Journal.Core.Models;

namespace Journal.Core.Storage;

public sealed class ImageStore : IImageStore
{
    private static readonly string[] SupportedExtensions = [".png", ".bmp", ".jpg", ".jpeg"];

    private readonly JournalLocations _locations;

    public ImageStore(JournalLocations locations)
    {
        _locations = locations;
    }

    public static IReadOnlyList<string> Extensions => SupportedExtensions;

    public IReadOnlyList<JournalImage> GetImages(string journalName)
    {
        var folder = _locations.GetImagesFolder(journalName);
        if (!Directory.Exists(folder))
        {
            return [];
        }

        return Directory.EnumerateFiles(folder)
            .Where(path => IsSupported(path))
            .Select(path => new JournalImage(Path.GetFileName(path), path, File.GetLastWriteTime(path)))
            .OrderByDescending(image => image.AddedOn)
            .ThenBy(image => image.FileName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public JournalImage AddImage(string journalName, string fileName, Stream content)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!SupportedExtensions.Contains(extension))
        {
            throw new JournalException($"'{fileName}' is not a supported image. Use .png, .bmp or .jpg.");
        }

        var baseName = FileNameSanitizer.Sanitize(Path.GetFileNameWithoutExtension(fileName));
        if (baseName.Length == 0)
        {
            baseName = "Image";
        }

        var folder = _locations.GetImagesFolder(journalName);
        Directory.CreateDirectory(folder);
        var path = FileNameSanitizer.GetUniquePath(folder, baseName, extension);

        using (var destination = File.Create(path))
        {
            content.CopyTo(destination);
        }

        return new JournalImage(Path.GetFileName(path), path, File.GetLastWriteTime(path));
    }

    public void DeleteImage(JournalImage image)
    {
        File.Delete(image.FilePath);
    }

    private static bool IsSupported(string path)
    {
        return SupportedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
    }
}
