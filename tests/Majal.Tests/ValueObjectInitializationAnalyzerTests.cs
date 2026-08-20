using System.Collections.Immutable;
using Majal.Common.Analyzers;
using Majal.Common.CodeFixes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Majal.Tests;

public class ValueObjectInitializationAnalyzerTests
{
    [Fact]
    public async Task Analyzer_ShouldReportError_WhenUsingNewOutsideType()
    {
        const string source =
            """
            using Majal;

            [ValueObject]
            public partial class Email
            {
                public string Value { get; }

                public Email(string value) { Value = value; }

                public static Email Create(string value) => new(value);
            }

            public class Consumer
            {
                public Email GetEmail() => new Email("test@example.com");
            }
            """;

        var diagnostics = await GetDiagnostics(source);

        Assert.Contains(diagnostics, d => d.Id == ValueObjectInitializationAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task Analyzer_ShouldNotReportError_WhenUsingNewInsideOwnType()
    {
        const string source =
            """
            using Majal;

            [ValueObject]
            public partial class Email
            {
                public string Value { get; }

                public Email(string value) { Value = value; }

                public static Email Create(string value) => new(value);
            }
            """;

        var diagnostics = await GetDiagnostics(source);

        Assert.DoesNotContain(diagnostics, d => d.Id == ValueObjectInitializationAnalyzer.DiagnosticId);
    }

    [Fact]
    public async Task CodeFix_ShouldReplaceWithFactoryMethodInvocation()
    {
        const string source =
            """
            using Majal;

            [ValueObject]
            public partial class Email
            {
                public string Value { get; }

                public Email(string value) { Value = value; }

                public static Email Create(string value) => new(value);
            }

            public class Consumer
            {
                public Email GetEmail() => new Email("test@example.com");
            }
            """;

        var (newSource, _) = await ApplyCodeFix(source);

        Assert.Contains("Email.Create(\"test@example.com\")", newSource);
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

        var compilationWithAnalyzers = compilation.WithAnalyzers([new ValueObjectInitializationAnalyzer()]);
        return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
    }

    private static async Task<(string, ImmutableArray<Diagnostic>)> ApplyCodeFix(string source)
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

        var analyzer = new ValueObjectInitializationAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers([analyzer]);
        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        var adhocWorkspace = new AdhocWorkspace();
        var project = adhocWorkspace.AddProject("Test", LanguageNames.CSharp)
            .AddMetadataReferences(references);
        var document = project.AddDocument("Test.cs", source);

        var codeFixProvider = new ValueObjectInitializationCodeFix();
        var fix = diagnostics.FirstOrDefault(d => d.Id == ValueObjectInitializationAnalyzer.DiagnosticId);

        if (fix == null) return (source, diagnostics);

        var actions = new List<CodeAction>();
        var context = new CodeFixContext(document, fix, (a, d) => actions.Add(a), CancellationToken.None);
        await codeFixProvider.RegisterCodeFixesAsync(context);

        var action = actions.First();
        var operations = await action.GetOperationsAsync(CancellationToken.None);
        var editOperation = operations.OfType<ApplyChangesOperation>().First();
        var changedDocument = editOperation.ChangedSolution.GetDocument(document.Id);

        if (changedDocument is null) return ("", diagnostics);

        var newSource = await changedDocument.GetTextAsync();
        return (newSource.ToString(), diagnostics);
    }
}