namespace Journal.Core.Rendering;

public enum LinkKind
{
    Web,
    Document
}

/// <summary>What a link should open: a web address, or the local/network path of a document.</summary>
public sealed record LinkTarget(LinkKind Kind, string Value);

/// <summary>
/// Turns what the user types into the href stored in a note, and turns a stored href back into something that can be
/// opened. Notes live on a shared drive, so only web addresses and a fixed set of document types are ever opened;
/// anything else (programs, scripts, other URL schemes) is refused.
/// </summary>
public static class LinkAddress
{
    private static readonly string[] WebSchemes = ["http", "https", "mailto"];

    public static IReadOnlyList<string> DocumentExtensions { get; } =
    [
        ".doc", ".docx", ".xls", ".xlsx", ".csv", ".pdf", ".ppt", ".pptx", ".txt", ".rtf"
    ];

    /// <summary>Accepts a web address (the scheme is optional) or a document path, and returns the href to store.</summary>
    public static string ToHref(string input)
    {
        var address = input.Trim().Trim('"');
        if (address.Length == 0)
        {
            throw new JournalException("Enter a web address or the path to a document.");
        }

        if (IsWindowsPath(address))
        {
            return ToDocumentHref(address);
        }

        if (Uri.TryCreate(address, UriKind.Absolute, out var absolute))
        {
            if (IsWebScheme(absolute.Scheme))
            {
                return absolute.AbsoluteUri;
            }

            if (absolute.IsFile)
            {
                return ToDocumentHref(absolute.LocalPath);
            }
        }

        if (!address.Contains("://")
            && Uri.TryCreate($"https://{address}", UriKind.Absolute, out var web)
            && web.Host.Contains('.'))
        {
            return web.AbsoluteUri;
        }

        throw new JournalException($"'{address}' is not a web address or a document path.");
    }

    /// <summary>Works out what a stored href opens. The href comes from a file on disk, so it is not trusted.</summary>
    public static LinkTarget Resolve(string href)
    {
        if (!Uri.TryCreate(href, UriKind.Absolute, out var uri))
        {
            throw new JournalException("This link is not a valid address.");
        }

        if (IsWebScheme(uri.Scheme))
        {
            return new LinkTarget(LinkKind.Web, uri.AbsoluteUri);
        }

        if (uri.IsFile)
        {
            EnsureDocumentExtension(uri.LocalPath);
            return new LinkTarget(LinkKind.Document, uri.LocalPath);
        }

        throw new JournalException($"Links to '{uri.Scheme}' addresses cannot be opened.");
    }

    private static string ToDocumentHref(string path)
    {
        EnsureDocumentExtension(path);
        try
        {
            return new Uri(path).AbsoluteUri;
        }
        catch (UriFormatException)
        {
            throw new JournalException($"'{path}' is not a valid path.");
        }
    }

    private static void EnsureDocumentExtension(string path)
    {
        var isDocument = DocumentExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
        if (!isDocument)
        {
            throw new JournalException($"Only these document types can be linked: {string.Join(", ", DocumentExtensions)}");
        }
    }

    private static bool IsWebScheme(string scheme)
    {
        return WebSchemes.Contains(scheme, StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsWindowsPath(string address)
    {
        var isUnc = address.StartsWith(@"\\", StringComparison.Ordinal);
        var isDriveLetter = address.Length >= 3
            && char.IsAsciiLetter(address[0])
            && address[1] == ':'
            && (address[2] == '\\' || address[2] == '/');
        return isUnc || isDriveLetter;
    }
}
