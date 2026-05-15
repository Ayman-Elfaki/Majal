using Majal.Analyzers;
using Majal.CodeFixes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Majal.Tests;

public class GetEqualityComponentsCodeFixTests
{
    [Fact]
    public async Task CodeFix_ShouldImplementGetEqualityComponents_ForClass()
    {
        const string source =
            """
            using Majal;

            [ValueObject]
            public partial class UserProfile
            {
                public string Name { get; }
                public int Age { get; }
            }
            """;

        var (newSource, _) = await ApplyCodeFix(source);

        Assert.Contains("private IEnumerable<object?> GetEqualityComponents()", newSource);
        Assert.Contains("yield return Name;", newSource);
        Assert.Contains("yield return Age;", newSource);
    }

    [Fact]
    public async Task CodeFix_ShouldImplementGetEqualityComponents_ForStruct()
    {
        const string source =
            """
            using Majal;

            [ValueObject]
            public partial struct Coordinates
            {
                public double X { get; }
                public double Y { get; }
            }
            """;

        var (newSource, _) = await ApplyCodeFix(source);

        Assert.Contains("private IEnumerable<object?> GetEqualityComponents()", newSource);
        Assert.Contains("yield return X;", newSource);
        Assert.Contains("yield return Y;", newSource);
    }

    private static async Task<(string, ImmutableArray<Diagnostic>)> ApplyCodeFix(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        MetadataReference[] references =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ValueObjectAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("netstandard").Location),
            MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("System.Runtime").Location),
            MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("System.Collections").Location)
        ];

        var compilation = CSharpCompilation.Create("Test")
            .AddReferences(references)
            .AddSyntaxTrees(syntaxTree);

        var analyzer = new GetEqualityComponentsAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers([analyzer]);
        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        var adhocWorkspace = new AdhocWorkspace();
        var project = adhocWorkspace.AddProject("Test", LanguageNames.CSharp)
            .AddMetadataReferences(references);
        var document = project.AddDocument("Test.cs", source);

        var codeFixProvider = new GetEqualityComponentsCodeFix();
        var fix = diagnostics.FirstOrDefault(d => d.Id == GetEqualityComponentsAnalyzer.DiagnosticId);

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
