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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ValueObjectInitializationCodeFix)), Shared]
public sealed class ValueObjectInitializationCodeFix : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(ValueObjectInitializationAnalyzer.DiagnosticId);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null) return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var node = root.FindNode(diagnosticSpan, getInnermostNodeForTie: true);
        if (node is not BaseObjectCreationExpressionSyntax)
        {
             // Try parent if we found something like an identifier or argument list
             node = node.AncestorsAndSelf().OfType<BaseObjectCreationExpressionSyntax>().FirstOrDefault();
        }

        if (node is BaseObjectCreationExpressionSyntax objectCreation)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: $"Use {ValueObjectTemplate.FactoryMethodName} factory method",
                    createChangedDocument: c => UseFactoryMethodAsync(context.Document, objectCreation, c),
                    equivalenceKey: "UseFactoryMethod"),
                diagnostic);
        }
    }

    private static async Task<Document> UseFactoryMethodAsync(Document document, BaseObjectCreationExpressionSyntax objectCreation, CancellationToken ct)
    {
        var semanticModel = await document.GetSemanticModelAsync(ct).ConfigureAwait(false);
        if (semanticModel == null) return document;

        var typeInfo = semanticModel.GetTypeInfo(objectCreation, ct);
        if (typeInfo.Type is not INamedTypeSymbol namedType) return document;

        var typeName = namedType.Name;
        
        // Build the invocation: TypeName.From(arguments)
        var memberAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(typeName),
            SyntaxFactory.IdentifierName(ValueObjectTemplate.FactoryMethodName));

        var invocation = SyntaxFactory.InvocationExpression(memberAccess)
            .WithArgumentList(objectCreation.ArgumentList ?? SyntaxFactory.ArgumentList());

        var editor = await DocumentEditor.CreateAsync(document, ct).ConfigureAwait(false);
        editor.ReplaceNode(objectCreation, invocation.WithTriviaFrom(objectCreation));

        return editor.GetChangedDocument();
    }
}
