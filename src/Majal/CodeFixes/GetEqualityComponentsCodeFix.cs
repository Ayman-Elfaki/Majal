using System.Collections.Immutable;
using System.Composition;
using Majal.Abstractions;
using Majal.Analyzers;
using Majal.Templates;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using static Majal.Abstractions.Constants;

namespace Majal.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(GetEqualityComponentsCodeFix)), Shared]
public sealed class GetEqualityComponentsCodeFix : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(GetEqualityComponentsAnalyzer.DiagnosticId);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.First();
        context.RegisterCodeFix(
            CodeAction.Create(
                title: $"Implement {ValueObjectTemplate.EqualityMethodName}",
                createChangedDocument: c => ImplementMethodAsync(context.Document, diagnostic, c),
                equivalenceKey: $"Implement{ValueObjectTemplate.EqualityMethodName}"),
            diagnostic);

        return Task.CompletedTask;
    }

    private static async Task<Document> ImplementMethodAsync(Document document, Diagnostic diagnostic,
        CancellationToken ct)
    {
        var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);
        if (root == null) return document;

        var node = root.FindNode(diagnostic.Location.SourceSpan);
        var typeDeclaration = node as TypeDeclarationSyntax ??
                              node.AncestorsAndSelf()
                                  .OfType<TypeDeclarationSyntax>()
                                  .FirstOrDefault(t => t is StructDeclarationSyntax or ClassDeclarationSyntax);

        if (typeDeclaration == null) return document;

        var semanticModel = await document.GetSemanticModelAsync(ct).ConfigureAwait(false);
        if (semanticModel == null) return document;

        var symbol = semanticModel.GetDeclaredSymbol(typeDeclaration, ct);
        if (symbol == null) return document;

        // gather property names and types
        var props = symbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p is
                { IsComputed: false, IsStatic: false, GetMethod.DeclaredAccessibility: Accessibility.Public })
            .Select(p => (p.Name, Type: p.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)))
            .ToList();

        var tupleTypes = string.Join(", ", props.Select(p => p.Type));
        var tupleValues = string.Join(", ", props.Select(p => p.Name));


        var returnType = props.Count > 0
            ? SyntaxFactory.ParseTypeName($"ValueTuple<{tupleTypes}>")
            : SyntaxFactory.ParseTypeName("ValueTuple");

        var expressionBody = SyntaxFactory.ArrowExpressionClause(SyntaxFactory.ParseExpression($"new({tupleValues})"));

        // create method declaration
        var method = SyntaxFactory.MethodDeclaration(returnType, ValueObjectTemplate.EqualityMethodName)
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
            .WithExpressionBody(expressionBody)
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
            .WithLeadingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed);

        var editor = await DocumentEditor.CreateAsync(document, ct).ConfigureAwait(false);
        var newTypeDeclaration = editor.Generator.AddMembers(typeDeclaration, method);
        editor.ReplaceNode(typeDeclaration, newTypeDeclaration);

        return editor.GetChangedDocument();
    }
}