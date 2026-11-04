using Journal.Core.Rendering;

namespace Journal.Tests;

public sealed class DiagramThemeDirectiveTests
{
    [Fact]
    public void GetTheme_ReturnsNullWithoutDirective()
    {
        Assert.Null(DiagramThemeDirective.GetTheme("flowchart TD\n A-->B"));
    }

    [Theory]
    [InlineData("%%{init: {\"theme\": \"forest\"}}%%\nflowchart TD", "forest")]
    [InlineData("%%{init: {'theme':'dark'}}%%\nflowchart TD", "dark")]
    [InlineData("%%{init: {\"themeVariables\": {\"primaryColor\": \"#fff\"}}}%%\nflowchart TD", null)]
    public void GetTheme_ReadsTheDirective(string text, string? expected)
    {
        Assert.Equal(expected, DiagramThemeDirective.GetTheme(text));
    }

    [Fact]
    public void SetTheme_AddsDirectiveAsFirstLine()
    {
        var result = DiagramThemeDirective.SetTheme("flowchart TD\n A-->B", "forest");

        Assert.Equal("%%{init: {\"theme\": \"forest\"}}%%\nflowchart TD\n A-->B", result);
    }

    [Fact]
    public void SetTheme_ReplacesExistingTheme()
    {
        var result = DiagramThemeDirective.SetTheme("%%{init: {'theme':'dark'}}%%\nflowchart TD", "neutral");

        Assert.Equal("neutral", DiagramThemeDirective.GetTheme(result));
        Assert.Equal("%%{init: {\"theme\": \"neutral\"}}%%\nflowchart TD", result);
    }

    [Fact]
    public void SetTheme_KeepsOtherSettings()
    {
        var text = "%%{init: {\"themeVariables\": {\"primaryColor\": \"#fff\"}}}%%\nflowchart TD";

        var result = DiagramThemeDirective.SetTheme(text, "base");

        Assert.Equal("%%{init: {\"theme\": \"base\", \"themeVariables\": {\"primaryColor\": \"#fff\"}}}%%\nflowchart TD", result);
    }

    [Fact]
    public void SetTheme_WithNull_RemovesDirectiveWhenOnlyThemeWasSet()
    {
        var result = DiagramThemeDirective.SetTheme("%%{init: {\"theme\": \"forest\"}}%%\nflowchart TD", null);

        Assert.Equal("flowchart TD", result);
    }

    [Fact]
    public void SetTheme_WithNull_KeepsOtherSettings()
    {
        var text = "%%{init: {\"theme\": \"base\", \"themeVariables\": {\"primaryColor\": \"#fff\"}}}%%\nflowchart TD";

        var result = DiagramThemeDirective.SetTheme(text, null);

        Assert.Equal("%%{init: {\"themeVariables\": {\"primaryColor\": \"#fff\"}}}%%\nflowchart TD", result);
    }

    [Fact]
    public void SetTheme_WithNull_LeavesTextWithoutDirectiveUnchanged()
    {
        Assert.Equal("flowchart TD", DiagramThemeDirective.SetTheme("flowchart TD", null));
    }
}
