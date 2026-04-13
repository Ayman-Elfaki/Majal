using System.Collections.Immutable;
using System.Composition;
using Majal.Analyzers;
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
                title: "Implement GetEqualityComponents",
                createChangedDocument: c => ImplementMethodAsync(context.Document, diagnostic, c),
                equivalenceKey: "ImplementGetEqualityComponents"),
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
                        node.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().FirstOrDefault();

        if (classDecl == null) return document;

        var semanticModel = await document.GetSemanticModelAsync(ct).ConfigureAwait(false);
        if (semanticModel == null) return document;

        var classSymbol = semanticModel.GetDeclaredSymbol(classDecl, ct);
        if (classSymbol == null) return document;

        // gather property names and types
        var props = classSymbol.GetMembers().OfType<IPropertySymbol>()
            .Where(p => p.GetMethod?.DeclaredAccessibility == Accessibility.Public)
            .Select(p => (p.Name, Type: p.Type.ToDisplayString()))
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
        var method = SyntaxFactory.MethodDeclaration(returnType, "GetEqualityComponents")
            .WithModifiers(SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                SyntaxFactory.Token(SyntaxKind.PartialKeyword)))
            .WithExpressionBody(expressionBody)
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
            .WithLeadingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed);

        var editor = await DocumentEditor.CreateAsync(document, ct).ConfigureAwait(false);
        var newClass = editor.Generator.AddMembers(classDecl, method);
        editor.ReplaceNode(classDecl, newClass);

        return editor.GetChangedDocument();
    }
}
