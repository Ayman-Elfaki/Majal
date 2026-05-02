using System.Collections.Immutable;
using Majal.Abstractions;
using Majal.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Majal.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ValueObjectAdditionalPropertiesAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "MJ006";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Generic ValueObject should not have additional properties",
        messageFormat: "Type '{0}' is marked with [ValueObject<T>] and should not have additional properties. Use the non-generic [ValueObject] instead if multiple properties are needed.",
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

        if (namedType.TypeKind != TypeKind.Struct && namedType.TypeKind != TypeKind.Class) return;

        // look for ValueObjectAttribute with a generic argument
        var valueAttr = namedType.GetAttributes()
            .FirstOrDefault(a =>
                a.AttributeClass is { Name: ValueObjectGenerator.ValueObjectAttributeName, IsGenericType: true } &&
                a.AttributeClass.ContainingNamespace?.ToDisplayString() == ValueObjectGenerator.AttributeNamespace);

        if (valueAttr == null) return;

        // check for public instance properties which are considered additional in a generic ValueObject
        var hasAdditionalProperties = namedType.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.Name != "Value")
            .Any(p => p is { DeclaredAccessibility: Accessibility.Public, IsStatic: false, IsComputed: false });

        if (!hasAdditionalProperties) return;

        // report diagnostic on the type identifier
        if (namedType.Locations.FirstOrDefault() is not { IsInSource: true } location) return;
        context.ReportDiagnostic(Diagnostic.Create(Rule, location, namedType.Name));
    }
}
