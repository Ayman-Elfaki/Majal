using System.Collections.Immutable;
using System.Composition;
using Majal.Analyzers;
using Majal.Templates;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

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
        var structDecl = node as StructDeclarationSyntax ??
                         node.AncestorsAndSelf()
                             .OfType<StructDeclarationSyntax>()
                             .FirstOrDefault();

        if (structDecl == null) return document;

        var semanticModel = await document.GetSemanticModelAsync(ct).ConfigureAwait(false);
        if (semanticModel == null) return document;

        var symbol = semanticModel.GetDeclaredSymbol(structDecl, ct);
        if (symbol == null) return document;

        // gather property names and types
        var props = symbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.GetMethod?.DeclaredAccessibility == Accessibility.Public)
            .Select(p => (p.Name, Type: p.Type.Name))
            .ToList();

        TypeSyntax returnType;
        ArrowExpressionClauseSyntax expressionBody;

        if (props.Count == 1)
        {
            returnType = SyntaxFactory.ParseTypeName(props[0].Type);
            expressionBody = SyntaxFactory.ArrowExpressionClause(SyntaxFactory.ParseExpression(props[0].Name));
        }
        else
        {
            var tupleTypes = string.Join(", ", props.Select(p => p.Type));
            var tupleValues = string.Join(", ", props.Select(p => p.Name));
            returnType = SyntaxFactory.ParseTypeName($"({tupleTypes})");
            expressionBody = SyntaxFactory.ArrowExpressionClause(SyntaxFactory.ParseExpression($"({tupleValues})"));
        }

        // create method declaration
        var method = SyntaxFactory.MethodDeclaration(returnType, ValueObjectTemplate.EqualityMethodName)
            .WithModifiers(SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                SyntaxFactory.Token(SyntaxKind.PartialKeyword)))
            .WithExpressionBody(expressionBody)
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
            .WithLeadingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed);

        var editor = await DocumentEditor.CreateAsync(document, ct).ConfigureAwait(false);
        var newStruct = editor.Generator.AddMembers(structDecl, method);
        editor.ReplaceNode(structDecl, newStruct);

        return editor.GetChangedDocument();
    }
}