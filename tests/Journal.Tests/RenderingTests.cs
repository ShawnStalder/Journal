using Journal.Core.Rendering;

namespace Journal.Tests;

public sealed class RenderingTests
{
    private readonly NoteHtmlSanitizer _sanitizer = new();

    [Fact]
    public void Sanitize_KeepsEditorFormatting()
    {
        var html = "<p><b>bold</b> <i>italic</i> <span style=\"color: rgb(255, 0, 0);\">red</span></p><ul><li>item</li></ul>";

        var result = _sanitizer.Sanitize(html);

        Assert.Contains("<b>bold</b>", result);
        Assert.Contains("<i>italic</i>", result);
        Assert.Contains("color", result);
        Assert.Contains("<li>item</li>", result);
    }

    [Fact]
    public void Sanitize_RemovesScriptsAndEventHandlers()
    {
        var html = "<p onclick=\"steal()\">hi</p><script>alert(1)</script><img src=\"x\" onerror=\"steal()\">";

        var result = _sanitizer.Sanitize(html);

        Assert.DoesNotContain("script", result);
        Assert.DoesNotContain("onclick", result);
        Assert.DoesNotContain("onerror", result);
        Assert.Contains("hi", result);
    }

    [Fact]
    public void Sanitize_KeepsEmbeddedImages()
    {
        var html = "<img src=\"data:image/png;base64,AAAA\">";

        var result = _sanitizer.Sanitize(html);

        Assert.Contains("data:image/png;base64,AAAA", result);
    }

    [Fact]
    public void ToHtml_MarksKeywordsStringsNumbersAndComments()
    {
        var result = SqlHighlighter.ToHtml("select 42, 'a' -- note");

        Assert.Contains("<span class=\"sql-keyword\">select</span>", result);
        Assert.Contains("<span class=\"sql-number\">42</span>", result);
        Assert.Contains("<span class=\"sql-string\">&#39;a&#39;</span>", result);
        Assert.Contains("<span class=\"sql-comment\">-- note</span>", result);
    }

    [Fact]
    public void ToHtml_EncodesHtmlInTheSql()
    {
        var result = SqlHighlighter.ToHtml("SELECT * FROM t WHERE a < b");

        Assert.Contains("&lt;", result);
        Assert.DoesNotContain("< b", result);
    }

    [Fact]
    public void ToHtml_DoesNotHighlightKeywordsInsideStringsOrIdentifiers()
    {
        var result = SqlHighlighter.ToHtml("SELECT 'from' FROM Selection");

        Assert.Equal(2, CountOccurrences(result, "sql-keyword"));
    }

    [Fact]
    public void ToHtml_HandlesAnUnterminatedBlockComment()
    {
        var result = SqlHighlighter.ToHtml("SELECT 1 /* open");

        Assert.Contains("<span class=\"sql-comment\">/* open</span>", result);
    }

    private static int CountOccurrences(string text, string value)
    {
        return text.Split(value).Length - 1;
    }
}
