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
    public void Sanitize_KeepsTablesWithHeaderSpanAndShading()
    {
        var html = "<table><tbody><tr><th colspan=\"2\">Head</th></tr><tr><td style=\"background-color: #fff176;\">a</td><td>b</td></tr></tbody></table>";

        var result = _sanitizer.Sanitize(html);

        Assert.Contains("<table>", result);
        Assert.Contains("<th colspan=\"2\">Head</th>", result);
        Assert.Contains("background-color", result);
        Assert.Contains("<td>b</td>", result);
    }

    [Fact]
    public void Sanitize_RemovesScriptAndHandlersInsideTableCells()
    {
        var html = "<table><tr><td onclick=\"steal()\">x<script>alert(1)</script></td></tr></table>";

        var result = _sanitizer.Sanitize(html);

        Assert.Contains("<td>x</td>", result);
        Assert.DoesNotContain("script", result);
        Assert.DoesNotContain("onclick", result);
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

    [Fact]
    public void Sanitize_KeepsCodeBlocksAndTheirLanguage()
    {
        var html = "<pre><code class=\"language-csharp\">var x = 1;\nif (x &lt; 2) { }</code></pre><p>use <code>var</code></p>";

        var result = _sanitizer.Sanitize(html);

        Assert.Contains("<pre><code class=\"language-csharp\">", result);
        Assert.Contains("x &lt; 2", result);
        Assert.Contains("<code>var</code>", result);
    }

    [Fact]
    public void Sanitize_DoesNotLetCodeTextBecomeMarkup()
    {
        var html = "<pre><code class=\"language-plaintext\">&lt;script&gt;alert(1)&lt;/script&gt;</code></pre>";

        var result = _sanitizer.Sanitize(html);

        Assert.DoesNotContain("<script", result);
        Assert.Contains("&lt;script&gt;", result);
    }

    [Fact]
    public void Sanitize_DropsClassesThatAreNotCodeLanguages()
    {
        var html = "<pre class=\"evil\"><code class=\"language-sql evil\">SELECT 1</code></pre>";

        var result = _sanitizer.Sanitize(html);

        Assert.DoesNotContain("evil", result);
        Assert.Contains("language-sql", result);
    }

    [Fact]
    public void CodeLanguages_HaveUniqueLowercaseIds()
    {
        var ids = CodeLanguages.All
            .Select(language => language.Id)
            .ToList();

        Assert.Equal(ids.Count, ids.Distinct().Count());
        Assert.All(ids, id => Assert.Equal(id.ToLowerInvariant(), id));
    }

    private static int CountOccurrences(string text, string value)
    {
        return text.Split(value).Length - 1;
    }
}
