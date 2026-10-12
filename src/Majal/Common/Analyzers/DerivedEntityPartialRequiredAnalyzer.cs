using System.Collections.Immutable;
using Majal.Common.Abstractions;
using Majal.Generators.Entities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Majal.Common.Analyzers;

/// <summary>
/// Reports derived entity classes that declare navigation properties but are not <c>partial</c>,
/// which prevents the generator from emitting their foreign key properties.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DerivedEntityPartialRequiredAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "MJ007";

    private static readonly DiagnosticDescriptor Rule = new(
        id: DiagnosticId,
        title: "Derived entity class must be partial to generate foreign keys",
        messageFormat:
        "Class '{0}' derives from an entity and has navigation properties, so it should be declared 'partial' for foreign keys to be generated",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
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

        if (namedType.TypeKind != TypeKind.Class) return;

        // types with their own entity attribute are covered by the entity generator
        if (namedType.HasAnyMajaAttribute(EntityGenerator.EntityAttributeName)) return;

        // must derive from an entity
        if (namedType.BaseType?.FindAttributedTypeInHierarchy(EntityGenerator.EntityAttributeName) is null) return;

        // already partial: nothing to report
        if (namedType.DeclaringSyntaxReferences.Any(IsPartial)) return;

        if (!namedType.GetMembers().OfType<IPropertySymbol>().Any(IsNavigationProperty)) return;

        if (namedType.Locations.FirstOrDefault() is not { IsInSource: true } location) return;
        context.ReportDiagnostic(Diagnostic.Create(Rule, location, namedType.Name));
    }

    private static bool IsPartial(SyntaxReference reference)
    {
        return reference.GetSyntax() is Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax declaration &&
               declaration.Modifiers.Any(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword);
    }

    private static bool IsNavigationProperty(IPropertySymbol property)
    {
        if (property.IsStatic || property.IsIndexer) return false;
        if (property.DeclaredAccessibility != Accessibility.Public) return false;
        if (property.GetMethod is null || property.SetMethod is null) return false;
        if (property.GetAttributes().Any(a => a.AttributeClass?.Name is "NotMappedAttribute" or "NotMapped"))
            return false;
        if (property.Type.GetCollectionInfo().IsCollection) return false;

        var (unwrapped, _) = property.Type.UnwrapNullable();
        if (unwrapped is not INamedTypeSymbol target) return false;

        return target.FindAttributedTypeInHierarchy(EntityGenerator.EntityAttributeName) is not null ||
               target.HasAnyMajaAttribute("AggregateAttribute");
    }
}
