using Journal.Core;
using Journal.Core.Rendering;

namespace Journal.Tests;

public sealed class LinkAddressTests
{
    private readonly NoteHtmlSanitizer _sanitizer = new();

    [Theory]
    [InlineData("https://example.com/page", "https://example.com/page")]
    [InlineData("http://example.com/a?b=1", "http://example.com/a?b=1")]
    [InlineData("  https://example.com/page  ", "https://example.com/page")]
    [InlineData("mailto:someone@example.com", "mailto:someone@example.com")]
    public void ToHref_KeepsWebAddresses(string input, string expected)
    {
        Assert.Equal(expected, LinkAddress.ToHref(input));
    }

    [Fact]
    public void ToHref_AddsHttpsWhenTheSchemeIsMissing()
    {
        Assert.Equal("https://www.example.com/", LinkAddress.ToHref("www.example.com"));
    }

    [Fact]
    public void ToHref_ConvertsALocalPathToAFileAddress()
    {
        var href = LinkAddress.ToHref(@"C:\My Docs\Report #1.docx");

        Assert.Equal("file:///C:/My%20Docs/Report%20%231.docx", href);
    }

    [Fact]
    public void ToHref_ConvertsANetworkPathToAFileAddress()
    {
        var href = LinkAddress.ToHref(@"\\server\share\Budget.xlsx");

        Assert.Equal("file://server/share/Budget.xlsx", href);
    }

    [Fact]
    public void ToHref_IgnoresQuotesAddedByCopyAsPath()
    {
        var href = LinkAddress.ToHref("\"C:\\Docs\\Manual.pdf\"");

        Assert.Equal("file:///C:/Docs/Manual.pdf", href);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("javascript:alert(1)")]
    [InlineData("not a link")]
    [InlineData(@"C:\Tools\setup.exe")]
    [InlineData(@"\\server\share\run.bat")]
    [InlineData("file:///C:/Tools/setup.exe")]
    [InlineData("ftp://example.com/file")]
    public void ToHref_RejectsAddressesThatCannotBeLinked(string input)
    {
        Assert.Throws<JournalException>(() => LinkAddress.ToHref(input));
    }

    [Fact]
    public void Resolve_ReturnsWebAddresses()
    {
        var target = LinkAddress.Resolve("https://example.com/");

        Assert.Equal(new LinkTarget(LinkKind.Web, "https://example.com/"), target);
    }

    [Fact]
    public void Resolve_ReturnsTheLocalPathOfADocument()
    {
        var target = LinkAddress.Resolve("file:///C:/My%20Docs/Report%20%231.docx");

        Assert.Equal(LinkKind.Document, target.Kind);
        Assert.Equal(@"C:\My Docs\Report #1.docx", target.Value);
    }

    [Fact]
    public void Resolve_ReturnsTheNetworkPathOfADocument()
    {
        var target = LinkAddress.Resolve("file://server/share/Budget.xlsx");

        Assert.Equal(@"\\server\share\Budget.xlsx", target.Value);
    }

    [Theory]
    [InlineData("file:///C:/Tools/setup.exe")]
    [InlineData("file:///C:/Tools/script.ps1")]
    [InlineData("javascript:alert(1)")]
    [InlineData("ms-msdt:/id PCWDiagnostic")]
    [InlineData("relative/path.docx")]
    public void Resolve_RefusesAnythingThatIsNotAWebAddressOrADocument(string href)
    {
        Assert.Throws<JournalException>(() => LinkAddress.Resolve(href));
    }

    [Fact]
    public void Sanitize_KeepsWebAndDocumentLinks()
    {
        var html = "<p><a href=\"https://example.com/\">site</a> <a href=\"file:///C:/Docs/Manual.pdf\">manual</a></p>";

        var result = _sanitizer.Sanitize(html);

        Assert.Contains("href=\"https://example.com/\"", result);
        Assert.Contains("href=\"file:///C:/Docs/Manual.pdf\"", result);
    }

    [Fact]
    public void Sanitize_RemovesScriptLinks()
    {
        var html = "<a href=\"javascript:alert(1)\">x</a>";

        var result = _sanitizer.Sanitize(html);

        Assert.DoesNotContain("javascript", result);
    }
}
