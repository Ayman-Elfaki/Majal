using System.Collections.Immutable;
using System.Composition;
using Majal.Analyzers;
using Majal.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Majal.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ValueObjectAdditionalPropertiesCodeFix)), Shared]
public sealed class ValueObjectAdditionalPropertiesCodeFix : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(ValueObjectAdditionalPropertiesAnalyzer.DiagnosticId);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null) return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var node = root.FindNode(diagnosticSpan);
        var typeDeclaration = node.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().FirstOrDefault();

        if (typeDeclaration == null) return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Use non-generic [ValueObject]",
                createChangedDocument: c => UseNonGenericValueObjectAsync(context.Document, typeDeclaration, c),
                equivalenceKey: "UseNonGenericValueObject"),
            diagnostic);
    }

    private static async Task<Document> UseNonGenericValueObjectAsync(Document document,
        TypeDeclarationSyntax typeDeclaration, CancellationToken ct)
    {
        var semanticModel = await document.GetSemanticModelAsync(ct).ConfigureAwait(false);
        if (semanticModel == null) return document;

        var symbol = semanticModel.GetDeclaredSymbol(typeDeclaration, ct);
        if (symbol == null) return document;

        var attribute = symbol.GetAttributes()
            .FirstOrDefault(a =>
                a.AttributeClass is { Name: ValueObjectGenerator.ValueObjectAttributeName, IsGenericType: true } &&
                a.AttributeClass.ContainingNamespace?.ToDisplayString() == ValueObjectGenerator.AttributeNamespace);

        if (attribute?.ApplicationSyntaxReference == null) return document;

        var attributeSyntax =
            (AttributeSyntax)await attribute.ApplicationSyntaxReference.GetSyntaxAsync(ct).ConfigureAwait(false);

        NameSyntax newName = attributeSyntax.Name switch
        {
            GenericNameSyntax genericName => SyntaxFactory.IdentifierName(genericName.Identifier.Text)
                .WithTriviaFrom(genericName),
            QualifiedNameSyntax { Right: GenericNameSyntax subGenericName } qualifiedName => qualifiedName.WithRight(
                SyntaxFactory.IdentifierName(subGenericName.Identifier.Text).WithTriviaFrom(subGenericName)),
            _ => SyntaxFactory.IdentifierName("ValueObject")
        };

        var newAttributeSyntax = attributeSyntax.WithName(newName)
            .WithArgumentList(null);

        var editor = await DocumentEditor.CreateAsync(document, ct).ConfigureAwait(false);
        editor.ReplaceNode(attributeSyntax, newAttributeSyntax);

        return editor.GetChangedDocument();
    }
}
