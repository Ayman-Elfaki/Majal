using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Majal.Generators.Aggregates.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AggregateAttributeCodeFixProvider)), Shared]
public sealed class AggregateAttributeCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(AggregateAttributeAnalyzer.DiagnosticId);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.First();
        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Mark class with Entity<T> attribute",
                createChangedDocument: c => ImplementMethodAsync(context.Document, diagnostic, c),
                equivalenceKey: "Mark class with Entity<T> attribute"),
            diagnostic);

        return Task.CompletedTask;
    }

    private static async Task<Document> ImplementMethodAsync(Document document, Diagnostic diagnostic,
        CancellationToken ct)
    {
        var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);
        if (root == null) return document;

        var node = root.FindNode(diagnostic.Location.SourceSpan);
        var classDecl = node as ClassDeclarationSyntax ??
                        node.AncestorsAndSelf()
                            .OfType<ClassDeclarationSyntax>()
                            .FirstOrDefault();

        if (classDecl == null) return document;

        var semanticModel = await document.GetSemanticModelAsync(ct).ConfigureAwait(false);
        if (semanticModel == null) return document;

        var classSymbol = semanticModel.GetDeclaredSymbol(classDecl, ct);
        if (classSymbol == null) return document;

        var editor = await DocumentEditor.CreateAsync(document, ct).ConfigureAwait(false);

        var genericAttribute =
            editor.Generator.GenericName("Entity", editor.Generator.TypeExpression(SpecialType.System_Int32))
                .WithoutTrailingTrivia();

        var attribute = editor.Generator.Attribute(genericAttribute, []);

        var newClass = editor.Generator.AddAttributes(classDecl, attribute);

        editor.ReplaceNode(classDecl, newClass);

        return editor.GetChangedDocument();
    }
}