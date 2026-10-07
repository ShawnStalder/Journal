using Ganss.Xss;

namespace Journal.Core.Rendering;

/// <summary>
/// Notes live on a shared drive and are rendered as raw HTML, so anything that could run script
/// is stripped before display. Formatting produced by the editor (bold, colors, lists) is kept.
/// </summary>
public sealed class NoteHtmlSanitizer
{
    private readonly HtmlSanitizer _sanitizer;

    public NoteHtmlSanitizer()
    {
        _sanitizer = new HtmlSanitizer();
        _sanitizer.AllowedSchemes.Add("data");
        _sanitizer.AllowedTags.Add("font");
        _sanitizer.AllowedAttributes.Add("color");
        _sanitizer.AllowedAttributes.Add("class");

        // Only the code-block language classes survive; any other class on a note is dropped.
        foreach (var language in CodeLanguages.All)
        {
            _sanitizer.AllowedClasses.Add(CodeLanguages.GetClassName(language.Id));
        }
    }

    public string Sanitize(string html)
    {
        return _sanitizer.Sanitize(html);
    }
}
