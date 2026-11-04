using System.Text.RegularExpressions;

namespace Journal.Core.Rendering;

/// <summary>
/// Reads and edits the theme in a diagram's leading <c>%%{init: {"theme": "..."}}%%</c> directive. Other settings in the
/// directive are left alone.
/// </summary>
public static class DiagramThemeDirective
{
    public static IReadOnlyList<string> Themes { get; } = ["default", "neutral", "dark", "forest", "base"];

    private static readonly Regex _directive = new(
        @"^(?<lead>\s*)%%\{\s*init\s*:\s*(?<body>\{.*\})\s*\}%%[ \t]*(?:\r?\n)?",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex _themePair = new(
        @"(?<quote>['""]?)theme\k<quote>\s*:\s*(?<valueQuote>['""])(?<value>[\w-]+)\k<valueQuote>",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex _emptyBody = new(@"^\{\s*\}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string? GetTheme(string text)
    {
        var directive = _directive.Match(text);
        if (!directive.Success)
        {
            return null;
        }

        var pair = _themePair.Match(directive.Groups["body"].Value);
        return pair.Success ? pair.Groups["value"].Value : null;
    }

    /// <summary>Sets the theme, or removes it when <paramref name="theme"/> is null or empty.</summary>
    public static string SetTheme(string text, string? theme)
    {
        var hasTheme = !string.IsNullOrWhiteSpace(theme);
        var directive = _directive.Match(text);

        if (!directive.Success)
        {
            return hasTheme ? $"{Build(theme!)}\n{text}" : text;
        }

        var body = directive.Groups["body"].Value;
        var newBody = hasTheme ? WithTheme(body, theme!) : WithoutTheme(body);

        var replacement = _emptyBody.IsMatch(newBody)
            ? string.Empty
            : $"{directive.Groups["lead"].Value}%%{{init: {newBody}}}%%\n";
        return replacement + text[directive.Length..];
    }

    private static string Build(string theme) => $"%%{{init: {{\"theme\": \"{theme}\"}}}}%%";

    private static string WithTheme(string body, string theme)
    {
        if (_themePair.IsMatch(body))
        {
            return _themePair.Replace(body, $"\"theme\": \"{theme}\"", 1);
        }

        var isEmpty = _emptyBody.IsMatch(body);
        return $"{{\"theme\": \"{theme}\"{(isEmpty ? string.Empty : ", ")}{body[1..].TrimStart()}";
    }

    private static string WithoutTheme(string body)
    {
        var withoutPair = Regex.Replace(
            _themePair.Replace(body, string.Empty, 1),
            @"\{\s*,\s*|\s*,\s*\}|,\s*,",
            match => match.Value.TrimStart().StartsWith('{') ? "{" : match.Value.TrimEnd().EndsWith('}') ? "}" : ",");
        return withoutPair;
    }
}
