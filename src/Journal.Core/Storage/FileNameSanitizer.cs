namespace Journal.Core.Storage;

public static class FileNameSanitizer
{
    private const int MaximumLength = 80;

    public static string Sanitize(string value)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var cleaned = new string(value
            .Select(character => invalidCharacters.Contains(character) ? ' ' : character)
            .ToArray());

        cleaned = string.Join(' ', cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        cleaned = cleaned.Trim('.', ' ');

        return cleaned.Length > MaximumLength
            ? cleaned[..MaximumLength].TrimEnd()
            : cleaned;
    }

    public static string GetUniquePath(string folder, string baseName, string extension)
    {
        var path = Path.Combine(folder, $"{baseName}{extension}");
        var counter = 2;
        while (File.Exists(path))
        {
            path = Path.Combine(folder, $"{baseName} ({counter}){extension}");
            counter++;
        }

        return path;
    }
}
