using Majal.Common.Abstractions;
using Microsoft.CodeAnalysis;

namespace Majal.Generators.Dtos.Services;

/// <summary>
/// Default implementation of type classification.
/// </summary>
public sealed class TypeClassifier : ITypeClassifier
{
    public bool IsValueObjectType(ITypeSymbol? typeSymbol)
    {
        if (typeSymbol is null) return false;

        var implementsValueObject = typeSymbol.AllInterfaces.Any(i =>
            i.MetadataName == "IValueObject" ||
            i.MetadataName.StartsWith("IValueObject`", StringComparison.Ordinal));

        return implementsValueObject || typeSymbol.HasAnyMajaAttribute(nameof(ValueObjectAttribute));
    }

    public bool IsEntityType(ITypeSymbol? typeSymbol)
    {
        if (typeSymbol is null) return false;

        var implementsEntity = typeSymbol.AllInterfaces.Any(i =>
            i.MetadataName.StartsWith("IEntity`", StringComparison.Ordinal));

        var hasEntityAttribute = typeSymbol.HasAnyMajaAttribute(nameof(EntityAttribute));
        return implementsEntity || hasEntityAttribute;
    }

    public bool IsAggregateType(ITypeSymbol? typeSymbol)
    {
        if (typeSymbol is null) return false;

        var implementsAggregate = typeSymbol.AllInterfaces.Any(i =>
            i.MetadataName.StartsWith("IAggregate`", StringComparison.Ordinal));

        return implementsAggregate || typeSymbol.HasAnyMajaAttribute(nameof(AggregateAttribute));
    }

    public ITypeSymbol? GetValueObjectUnderlyingType(INamedTypeSymbol namedType)
    {
        var genericValueObject = namedType.AllInterfaces
            .FirstOrDefault(i => i.MetadataName.StartsWith("IValueObject`", StringComparison.Ordinal));

        return genericValueObject?.TypeArguments.FirstOrDefault();
    }
}