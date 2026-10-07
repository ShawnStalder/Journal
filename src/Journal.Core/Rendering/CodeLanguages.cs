namespace Journal.Core.Rendering;

public sealed record CodeLanguage(string Id, string Label);

/// <summary>
/// Languages offered for code blocks. Ids match highlight.js language names; "auto" asks highlight.js to detect.
/// The labels are repeated in wwwroot/js/journal.js (languageLabels) for the badge shown on each block.
/// </summary>
public static class CodeLanguages
{
    public static IReadOnlyList<CodeLanguage> All { get; } =
    [
        new("csharp", "C#"),
        new("sql", "SQL"),
        new("json", "JSON"),
        new("xml", "XML / HTML"),
        new("javascript", "JavaScript"),
        new("typescript", "TypeScript"),
        new("powershell", "PowerShell"),
        new("yaml", "YAML"),
        new("bash", "Bash"),
        new("python", "Python"),
        new("css", "CSS"),
        new("plaintext", "Plain text"),
        new("auto", "Auto-detect")
    ];

    public static string GetClassName(string languageId) => $"language-{languageId}";
}
