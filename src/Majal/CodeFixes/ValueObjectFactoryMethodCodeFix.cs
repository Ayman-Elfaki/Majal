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

namespace Majal.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ValueObjectFactoryMethodCodeFix)), Shared]
public sealed class ValueObjectFactoryMethodCodeFix : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(ValueObjectFactoryMethodAnalyzer.DiagnosticId);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.First();
        context.RegisterCodeFix(
            CodeAction.Create(
                title: $"Implement {ValueObjectTemplate.FactoryMethodName} Factory Method",
                createChangedDocument: c => ImplementMethodAsync(context.Document, diagnostic, c),
                equivalenceKey: "ImplementFactoryMethod"),
            diagnostic);

        return Task.CompletedTask;
    }

    private static async Task<Document> ImplementMethodAsync(Document document, Diagnostic diagnostic,
        CancellationToken ct)
    {
        var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);
        if (root == null) return document;

        var node = root.FindNode(diagnostic.Location.SourceSpan);
        var structDeclaration = node as StructDeclarationSyntax ??
                                node.AncestorsAndSelf()
                                    .OfType<StructDeclarationSyntax>()
                                    .FirstOrDefault();

        if (structDeclaration == null) return document;

        var semanticModel = await document.GetSemanticModelAsync(ct).ConfigureAwait(false);
        if (semanticModel == null) return document;

        var symbol = semanticModel.GetDeclaredSymbol(structDeclaration, ct);
        if (symbol == null) return document;

        // gather property names
        var parameters = symbol.GetMembers().OfType<IPropertySymbol>()
            .Where(p => p is
                { GetMethod.DeclaredAccessibility: Accessibility.Public, IsStatic: false, IsComputed: false })
            .Select(p => SyntaxFactory.Parameter(SyntaxFactory.Identifier(ToCamelCase(p.Name)))
                .WithType(SyntaxFactory.ParseTypeName(p.Type.ToDisplayString()))
            ).ToArray();

        // build statements
        var statements = new List<StatementSyntax>
        {
            // generate throw new NotImplementedException
            SyntaxFactory.ParseStatement("throw new NotImplementedException();")
                .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed)
        };

        // create method declaration
        var returnType = SyntaxFactory.ParseTypeName(symbol.Name);
        var method = SyntaxFactory.MethodDeclaration(returnType, ValueObjectTemplate.FactoryMethodName)
            .AddParameterListParameters(parameters)
            .WithModifiers(SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
            .WithBody(SyntaxFactory.Block(statements))
            .WithLeadingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed);

        var editor = await DocumentEditor.CreateAsync(document, ct).ConfigureAwait(false);
        var newStruct = editor.Generator.AddMembers(structDeclaration, method);
        editor.ReplaceNode(structDeclaration, newStruct);

        return editor.GetChangedDocument();
    }

    private static string ToCamelCase(string str)
    {
        if (string.IsNullOrEmpty(str)) return str;
        return char.ToLower(str[0]) + str.Substring(1);
    }
}