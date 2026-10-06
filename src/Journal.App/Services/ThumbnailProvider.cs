using System.Collections.Concurrent;
using System.IO;
using System.Windows.Media.Imaging;
using Journal.Core.Models;

namespace Journal.App.Services;

public interface IThumbnailProvider
{
    /// <summary>Returns a small PNG data URI for the image, or null when it cannot be decoded.</summary>
    Task<string?> GetDataUriAsync(JournalImage image);
}

public sealed class ThumbnailProvider : IThumbnailProvider
{
    private const int ThumbnailWidth = 320;

    private readonly ConcurrentDictionary<(string Path, DateTime AddedOn), string?> _cache = new();
    // Limits concurrent reads so a large journal does not flood the network share.
    private readonly SemaphoreSlim _decodeSlots = new(3);

    public async Task<string?> GetDataUriAsync(JournalImage image)
    {
        var key = (image.FilePath, image.AddedOn);
        if (_cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        await _decodeSlots.WaitAsync();
        try
        {
            var dataUri = await Task.Run(() => CreateDataUri(image.FilePath));
            _cache[key] = dataUri;
            return dataUri;
        }
        finally
        {
            _decodeSlots.Release();
        }
    }

    private static string? CreateDataUri(string path)
    {
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = ThumbnailWidth;
            bitmap.UriSource = new Uri(path);
            bitmap.EndInit();
            bitmap.Freeze();

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = new MemoryStream();
            encoder.Save(stream);
            return $"data:image/png;base64,{Convert.ToBase64String(stream.ToArray())}";
        }
        catch (NotSupportedException)
        {
            return null;
        }
        catch (FormatException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}
