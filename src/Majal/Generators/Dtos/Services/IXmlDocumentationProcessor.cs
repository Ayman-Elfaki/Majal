using System.Text.RegularExpressions;

namespace Majal.Generators.Dtos.Services;

/// <summary>
/// Processes XML documentation from method and type symbols.
/// Provides extensibility for custom XML doc formatting.
/// </summary>
public interface IXmlDocumentationProcessor
{
    /// <summary>
    /// Formats raw XML documentation into properly formatted doc comments.
    /// </summary>
    string? FormatXmlDocs(string? xml);

    /// <summary>
    /// Extracts the summary section from XML documentation.
    /// </summary>
    string? ExtractSummary(string? xml);

    /// <summary>
    /// Extracts a specific parameter documentation from XML.
    /// </summary>
    string? ExtractParamDoc(string? xml, string paramName);
}
