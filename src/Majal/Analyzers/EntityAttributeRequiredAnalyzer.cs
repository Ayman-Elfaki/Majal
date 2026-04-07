using System.Collections.Immutable;
using Majal.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Majal.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EntityAttributeRequiredAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "MJ001";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "This class needs to be marked with Entity attribute as well",
        messageFormat: "Class '{0}' should be marked with [Entity<T>]",
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

        // look for Dependant Attributes
        var hasAttribute = namedType.GetAttributes()
            .Where(a => a.AttributeClass?.ContainingNamespace?.ToDisplayString() ==
                        AggregateGenerator.AttributeNamespace)
            .Any(a => a.AttributeClass is
            {
                Name: AggregateGenerator.AttributeName or AuditableGenerator.AttributeName
                or ArchivableGenerator.AttributeName or OrdinalGenerator.AttributeName
                or TranslatableGenerator.AttributeName
            });

        // look for EntityAttribute
        var entityAttr = namedType.GetAttributes()
            .FirstOrDefault(a =>
                a.AttributeClass?.Name == EntityGenerator.EntityAttributeName &&
                a.AttributeClass.ContainingNamespace?.ToDisplayString() == EntityGenerator.AttributeNamespace);

        if (!hasAttribute) return;

        // if attribute is present but Entity is not, report diagnostic
        if (entityAttr is not null) return;

        // report diagnostic on the type identifier
        if (namedType.Locations.FirstOrDefault() is not { IsInSource: true } location) return;
        context.ReportDiagnostic(Diagnostic.Create(Rule, location, namedType.Name));
    }
}