using System.Collections.Immutable;
using Majal.Abstractions;
using Majal.Generators;
using Majal.Templates;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Majal.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class GetEqualityComponentsAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "MJ003";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: $"Value object must implement {ValueObjectTemplate.EqualityMethodName}",
        messageFormat:
        $"Class '{{0}}' is marked with [ValueObject] and should provide an implementation of {ValueObjectTemplate.EqualityMethodName}",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        var namedType = (INamedTypeSymbol)context.Symbol;

        // only Struct or Class
        if (namedType.TypeKind != TypeKind.Struct && namedType.TypeKind != TypeKind.Class) return;

        // look for ValueObjectAttribute
        var valueAttr = namedType.GetAttributes()
            .FirstOrDefault(a =>
                a.AttributeClass is { Name: ValueObjectGenerator.ValueObjectAttributeName, IsGenericType: false } &&
                a.AttributeClass.ContainingNamespace?.ToDisplayString() == ValueObjectGenerator.AttributeNamespace);

        if (valueAttr == null) return;

        // check for public properties if none then ignore
        var hasPublicProperties = namedType.GetMembers()
            .OfType<IPropertySymbol>()
            .Any(p => p is { DeclaredAccessibility: Accessibility.Public, IsStatic: false, IsComputed: false });

        if (!hasPublicProperties) return;

        // examine members to find a method implementation
        var hasImplementation = namedType.GetMembers(ValueObjectTemplate.EqualityMethodName)
            .OfType<IMethodSymbol>()
            .Any(m => m.MethodKind == MethodKind.Ordinary &&
                      m.DeclaredAccessibility is Accessibility.Private &&
                      (m.PartialImplementationPart != null || m.DeclaringSyntaxReferences
                          .Select(r => r.GetSyntax())
                          .OfType<MethodDeclarationSyntax>()
                          .Any(s => s.Body != null || s.ExpressionBody != null)));

        if (hasImplementation) return;

        // report diagnostic on the type identifier
        if (namedType.Locations.FirstOrDefault() is not { IsInSource: true } location) return;
        context.ReportDiagnostic(Diagnostic.Create(Rule, location, namedType.Name));
    }
}