using Majal.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Majal.Tests;

public class GetEqualityComponentsAnalyzerTests
{
    [Fact]
    public async Task Analyzer_ShouldReportError_WhenClassMissingGetEqualityComponents()
    {
        const string source =
            """
            using Majal;

            [ValueObject]
            public partial class UserProfile
            {
                public string Name { get; }
            }
            """;

        var diagnostics = await GetDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == GetEqualityComponentsAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task Analyzer_ShouldNotReportError_WhenClassHasGetEqualityComponents()
    {
        const string source =
            """
            using Majal;

            [ValueObject]
            public partial class UserProfile
            {
                public string Name { get; }
                private (string) GetEqualityComponents() => new(Name);
            }
            """;

        var diagnostics = await GetDiagnostics(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == GetEqualityComponentsAnalyzer.DiagnosticId);
    }

    private static async Task<ImmutableArray<Diagnostic>> GetDiagnostics(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        MetadataReference[] references =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ValueObjectAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("netstandard").Location),
            MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("System.Runtime").Location)
        ];

        var compilation = CSharpCompilation.Create("Test")
            .AddReferences(references)
            .AddSyntaxTrees(syntaxTree);

        var compilationWithAnalyzers = compilation.WithAnalyzers([new GetEqualityComponentsAnalyzer()]);
        return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
    }
}