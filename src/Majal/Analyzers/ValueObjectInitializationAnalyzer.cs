using System.Collections.Immutable;
using Majal.Generators;
using Majal.Templates;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Majal.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ValueObjectInitializationAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "MJ005";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Value object should be initialized via factory method",
        messageFormat: "Type '{0}' should be initialized via the '{1}' factory method instead of 'new'",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, 
            SyntaxKind.ObjectCreationExpression, 
            SyntaxKind.ImplicitObjectCreationExpression);
    }

    private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
    {
        var objectCreation = (BaseObjectCreationExpressionSyntax)context.Node;
        
        var typeInfo = context.SemanticModel.GetTypeInfo(objectCreation, context.CancellationToken);
        if (typeInfo.Type is not INamedTypeSymbol namedType) return;

        // Only check structs and classes
        if (namedType.TypeKind != TypeKind.Struct && namedType.TypeKind != TypeKind.Class) return;

        // Check for [ValueObject] attribute
        var hasValueObjectAttribute = namedType.GetAttributes().Any(a =>
            a.AttributeClass is { Name: ValueObjectGenerator.ValueObjectAttributeName } &&
            a.AttributeClass.ContainingNamespace?.ToDisplayString() == ValueObjectGenerator.AttributeNamespace);

        if (!hasValueObjectAttribute) return;

        // Check if we are inside the same type (to allow factory methods to call the constructor)
        var enclosingSymbol = context.SemanticModel.GetEnclosingSymbol(objectCreation.SpanStart, context.CancellationToken);
        if (enclosingSymbol != null && SymbolEqualityComparer.Default.Equals(enclosingSymbol.ContainingType, namedType))
        {
            return;
        }

        // Check if the type actually has the factory method
        var hasFactoryMethod = namedType.GetMembers(ValueObjectTemplate.FactoryMethodName)
            .OfType<IMethodSymbol>()
            .Any(m => m is { IsStatic: true, DeclaredAccessibility: Accessibility.Public });

        if (!hasFactoryMethod) return;

        // Report diagnostic
        var location = objectCreation.GetLocation();
        context.ReportDiagnostic(Diagnostic.Create(Rule, location, namedType.Name, ValueObjectTemplate.FactoryMethodName));
    }
}
