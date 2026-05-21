using System.Text.RegularExpressions;

namespace Majal.Generators.Dtos.Services;

/// <summary>
/// Default implementation of XML documentation processing.
/// </summary>
public sealed class XmlDocumentationProcessor : IXmlDocumentationProcessor
{
    public string? FormatXmlDocs(string? xml)
    {
        if (string.IsNullOrWhiteSpace(xml)) return null;

        var lines = xml!.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var docLines = lines.Where(l => !l.TrimStart().StartsWith("<member") && !l.TrimStart().StartsWith("</member"));
        var formatted = string.Join("\n", docLines.Select(l => "/// " + l.TrimStart()));

        return string.IsNullOrWhiteSpace(formatted) ? null : formatted;
    }

    public string? ExtractSummary(string? xml)
    {
        if (string.IsNullOrWhiteSpace(xml)) return null;

        var match = Regex.Match(xml!, "<summary>(.*?)</summary>", RegexOptions.Singleline);
        if (!match.Success) return null;

        var content = match.Groups[1].Value.Trim();
        if (string.IsNullOrWhiteSpace(content)) return null;

        var lines = content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        return $"/// <summary>\n{string.Join("\n", lines.Select(l => "/// " + l.Trim()))}\n/// </summary>";
    }

    public string? ExtractParamDoc(string? xml, string paramName)
    {
        if (string.IsNullOrWhiteSpace(xml)) return null;

        var match = Regex.Match(xml!, $"""<param name="{paramName}">(.*?)</param>""", RegexOptions.Singleline);
        if (!match.Success) return null;

        var content = match.Groups[1].Value.Trim();
        if (string.IsNullOrWhiteSpace(content)) return null;

        var lines = content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        return $"/// <summary>\n{string.Join("\n", lines.Select(l => "/// " + l.Trim()))}\n/// </summary>";
    }
}
