using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Majal.Common.Analyzers;
using Xunit;

namespace Majal.Tests;

public class ValueObjectGenericArgumentAnalyzerTests
{
    [Fact]
    public async Task Analyzer_ShouldReportError_WhenGenericArgumentIsNotPrimitive()
    {
        const string source =
            """
            using Majal;
            using System.Collections.Generic;

            [ValueObject<List<int>>]
            public partial struct Tags
            {
            }
            """;

        var diagnostics = await GetDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == ValueObjectGenericArgumentAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task Analyzer_ShouldNotReportError_WhenGenericArgumentIsPrimitive()
    {
        const string source =
            """
            using Majal;

            [ValueObject<int>]
            public partial struct ProjectId
            {
            }
            """;

        var diagnostics = await GetDiagnostics(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == ValueObjectGenericArgumentAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task Analyzer_ShouldNotReportError_WhenNonGenericValueObject()
    {
        const string source =
            """
            using Majal;

            [ValueObject]
            public partial class Money
            {
                public decimal Amount { get; set; }

                public static Money Create(decimal amount) => new() { Amount = amount };
            }
            """;

        var diagnostics = await GetDiagnostics(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == ValueObjectGenericArgumentAnalyzer.DiagnosticId);
    }

    private static async Task<ImmutableArray<Diagnostic>> GetDiagnostics(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        MetadataReference[] references =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ValueObjectAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("netstandard").Location),
            MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("System.Runtime").Location)
        ];

        var compilation = CSharpCompilation.Create("Test")
            .AddReferences(references)
            .AddSyntaxTrees(syntaxTree);

        var compilationWithAnalyzers = compilation.WithAnalyzers([new ValueObjectGenericArgumentAnalyzer()]);
        return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
    }
}