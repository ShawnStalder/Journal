using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Journal.Core.Rendering;

/// <summary>Turns SQL text into HTML with token spans (sql-keyword, sql-string, sql-comment, sql-number).</summary>
public static partial class SqlHighlighter
{
    private static readonly HashSet<string> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "ADD", "ALL", "ALTER", "AND", "ANY", "AS", "ASC", "BEGIN", "BETWEEN", "BY", "CASE", "CAST", "CHECK",
        "COLUMN", "COMMIT", "CONSTRAINT", "CREATE", "CROSS", "CURSOR", "DATABASE", "DECLARE", "DEFAULT",
        "DELETE", "DESC", "DISTINCT", "DROP", "ELSE", "END", "EXEC", "EXECUTE", "EXISTS", "FOREIGN", "FROM",
        "FULL", "FUNCTION", "GO", "GROUP", "HAVING", "IF", "IN", "INDEX", "INNER", "INSERT", "INTO", "IS",
        "JOIN", "KEY", "LEFT", "LIKE", "NOT", "NULL", "ON", "OR", "ORDER", "OUTER", "OVER", "PARTITION",
        "PRIMARY", "PROCEDURE", "REFERENCES", "RETURN", "RETURNS", "RIGHT", "ROLLBACK", "SELECT", "SET",
        "TABLE", "THEN", "TOP", "TRANSACTION", "TRUNCATE", "UNION", "UNIQUE", "UPDATE", "USE", "VALUES",
        "VIEW", "WHEN", "WHERE", "WHILE", "WITH"
    };

    public static string ToHtml(string sql)
    {
        var builder = new StringBuilder();
        var position = 0;

        foreach (Match match in Tokens().Matches(sql))
        {
            AppendEncoded(builder, sql[position..match.Index]);
            AppendToken(builder, match);
            position = match.Index + match.Length;
        }

        AppendEncoded(builder, sql[position..]);
        return builder.ToString();
    }

    private static void AppendToken(StringBuilder builder, Match match)
    {
        var cssClass = GetCssClass(match);
        if (cssClass is null)
        {
            AppendEncoded(builder, match.Value);
            return;
        }

        builder.Append($"<span class=\"{cssClass}\">");
        AppendEncoded(builder, match.Value);
        builder.Append("</span>");
    }

    private static string? GetCssClass(Match match)
    {
        if (match.Groups["comment"].Success)
        {
            return "sql-comment";
        }

        if (match.Groups["string"].Success)
        {
            return "sql-string";
        }

        if (match.Groups["number"].Success)
        {
            return "sql-number";
        }

        return Keywords.Contains(match.Value) ? "sql-keyword" : null;
    }

    private static void AppendEncoded(StringBuilder builder, string text)
    {
        builder.Append(WebUtility.HtmlEncode(text));
    }

    [GeneratedRegex(@"(?<comment>--[^\r\n]*|/\*.*?(\*/|$))|(?<string>'(?:[^']|'')*'?)|(?<number>\b\d+(?:\.\d+)?\b)|(?<word>\b[A-Za-z_][A-Za-z0-9_]*\b)", RegexOptions.Singleline)]
    private static partial Regex Tokens();
}
