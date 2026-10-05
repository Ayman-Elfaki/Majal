using System.Collections.Immutable;
using Majal.Generators.ValueObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Majal.Common.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ValueObjectGenericArgumentAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "MJ004";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Value object generic argument must be a primitive data type or enum",
        messageFormat: "Class marked with [ValueObject] must have a primitive data type or enum as generic argument",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    private static readonly string[] SupportedTypes =
    [
        "Byte", "SByte", "Int16", "UInt16", "Int32", "UInt32", "Int64", "UInt64", "Boolean",
        "Single", "Double", "Decimal", "String", "DateTime", "DateOnly",
        "TimeOnly", "DateTimeOffset", "Guid", "Boolean", "Char"
    ];

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

        var isValid = IsValidGenericArgument(valueAttr.AttributeClass?.TypeArguments.FirstOrDefault());

        if (isValid) return;

        // report diagnostic on the type identifier
        if (namedType.Locations.FirstOrDefault() is not { IsInSource: true } location) return;
        context.ReportDiagnostic(Diagnostic.Create(Rule, location, namedType.Name));
    }

    private static bool IsValidGenericArgument(ITypeSymbol? type) =>
        type is not null && (type.TypeKind == TypeKind.Enum || IsPrimitive(type));

    private static bool IsPrimitive(ITypeSymbol? type) =>
        type?.Name is not null && SupportedTypes.Contains(type.Name, StringComparer.OrdinalIgnoreCase);
}