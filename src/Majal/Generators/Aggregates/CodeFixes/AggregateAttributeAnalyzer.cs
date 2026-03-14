using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Majal.Generators.Aggregates.CodeFixes;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AggregateAttributeAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "MJ001";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "A class marked with Aggregate attribute needs to be marked with Entity attribute as well",
        messageFormat:
        "Class '{0}' is marked with [Aggregate] and should be marked with [Entity<T>].",
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

        // only classes
        if (namedType.TypeKind != TypeKind.Class) return;

        // look for AggregateAttribute
        var aggregateRootAttr = namedType.GetAttributes()
            .FirstOrDefault(a =>
                a.AttributeClass?.Name == "AggregateAttribute" &&
                a.AttributeClass.ContainingNamespace?.ToDisplayString() == "Majal");


        // look for EntityAttribute
        var entityAttr = namedType.GetAttributes()
            .FirstOrDefault(a =>
                a.AttributeClass?.Name == "EntityAttribute" &&
                a.AttributeClass.ContainingNamespace?.ToDisplayString() == "Majal");

        if (aggregateRootAttr == null) return;

        // if Aggregate is present but Entity is not, report diagnostic
        if (entityAttr is not null) return;

        // report diagnostic on the type identifier
        if (namedType.Locations.FirstOrDefault() is not { IsInSource: true } location) return;
        context.ReportDiagnostic(Diagnostic.Create(Rule, location, namedType.Name));
    }
}