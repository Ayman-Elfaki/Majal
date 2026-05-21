using Majal.Common.Abstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Majal.Generators.Dtos.Services;

/// <summary>
/// Default implementation of context building.
/// </summary>
public sealed class DtoContextBuilder : IDtoContextBuilder
{
    private const string FlattenGenericAttributeName = $"{nameof(FlattenDtoForAttribute<>)}`1";

    public DtoForGenerator.DtoContext? BuildDtoContext(
        GeneratorAttributeSyntaxContext attributeContext,
        INamedTypeSymbol dtoSymbol,
        INamedTypeSymbol sourceSymbol,
        string attributeNamePrefix,
        string attributeNameSuffix,
        string factoryMethodName,
        string defaultMethodName
    )
    {
        var parentTypeDeclarations = GetParentTypeDeclarations(dtoSymbol);
        var nestedDtos = new Dictionary<string, DtoForGenerator.DtoData>();

        // Extract flatten configurations
        Dictionary<string, bool>? flattenConfigs = null;

        foreach (var flattenAttr in dtoSymbol.GetAttributes()
                     .Where(a => a.AttributeClass?.MetadataName == FlattenGenericAttributeName))
        {
            if (!(flattenAttr.AttributeClass?.TypeArguments.Length > 0)) continue;

            flattenConfigs ??= new Dictionary<string, bool>();
            var targetType = flattenAttr.AttributeClass.TypeArguments[0];
            var isReversed = flattenAttr.GetNamedArgumentValue<bool?>(nameof(FlattenDtoForAttribute<>.IsReversed))
                             ?? false;

            flattenConfigs[targetType.ToDisplayString()] = isReversed;
        }

        return new DtoForGenerator.DtoContext(
            IsRoot: true,
            Namespace: dtoSymbol.GetNamespace(),
            DtoName: dtoSymbol.GetTypeNameWithGenerics(),
            RawDtoName: dtoSymbol.Name,
            ParentTypeDeclarations: parentTypeDeclarations,
            DtoNamePrefix: attributeNamePrefix,
            DtoNameSuffix: attributeNameSuffix,
            Accessibility: dtoSymbol.DeclaredAccessibility,
            IsRecord: dtoSymbol.IsRecord,
            SourceSymbol: sourceSymbol,
            DefaultMethodName: defaultMethodName,
            FactoryMethodName: factoryMethodName,
            Collected: nestedDtos,
            FlattenConfigs: flattenConfigs,
            Compilation: attributeContext.SemanticModel.Compilation
        );
    }

    private static string[] GetParentTypeDeclarations(INamedTypeSymbol dtoSymbol)
    {
        var parentTypes = new List<string>();
        for (var current = dtoSymbol.ContainingType; current != null; current = current.ContainingType)
        {
            var typeKeyword = current.TypeKind switch
            {
                TypeKind.Struct when current.IsRecord => "record struct",
                TypeKind.Struct => "struct",
                TypeKind.Class when current.IsRecord => "record",
                _ => "class"
            };

            var modifier = current.IsStatic ? "static partial" : "partial";
            var accessModifier = current.DeclaredAccessibility switch
            {
                Accessibility.Private => "private",
                Accessibility.Internal => "internal",
                Accessibility.Protected => "protected",
                Accessibility.ProtectedOrInternal => "protected internal",
                Accessibility.ProtectedAndInternal => "private protected",
                _ => "public"
            };

            parentTypes.Add($"{accessModifier} {modifier} {typeKeyword} {current.GetTypeNameWithGenerics()}");
        }

        parentTypes.Reverse();

        return [.. parentTypes];
    }
}