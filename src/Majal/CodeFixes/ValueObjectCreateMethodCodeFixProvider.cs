using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Majal.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Majal.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ValueObjectCreateMethodCodeFixProvider)), Shared]
public sealed class ValueObjectCreateMethodCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(ValueObjectCreateMethodAnalyzer.DiagnosticId);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.First();
        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Implement Create Factory Method",
                createChangedDocument: c => ImplementMethodAsync(context.Document, diagnostic, c),
                equivalenceKey: "ImplementCreateFactoryMethod"),
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

        // gather property names
        var parameters = classSymbol.GetMembers().OfType<IPropertySymbol>()
            .Where(p => p.GetMethod?.DeclaredAccessibility == Accessibility.Public)
            .Select(p =>
                SyntaxFactory.Parameter(SyntaxFactory.Identifier(p.Name.SnakeCase))
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
        var returnType = SyntaxFactory.ParseTypeName(classSymbol.Name);
        var method = SyntaxFactory.MethodDeclaration(returnType, "Create")
            .AddParameterListParameters(parameters)
            .WithModifiers(SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                SyntaxFactory.Token(SyntaxKind.PartialKeyword)))
            .WithBody(SyntaxFactory.Block(statements))
            .WithLeadingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed);

        var editor = await DocumentEditor.CreateAsync(document, ct).ConfigureAwait(false);
        var newClass = editor.Generator.AddMembers(classDecl, method);
        editor.ReplaceNode(classDecl, newClass);

        return editor.GetChangedDocument();
    }
}