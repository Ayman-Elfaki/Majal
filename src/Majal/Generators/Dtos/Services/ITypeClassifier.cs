using Microsoft.CodeAnalysis;

namespace Majal.Generators.Dtos.Services;

/// <summary>
/// Classifies types based on their attributes, interfaces, and characteristics.
/// Provides extensibility for custom type classification logic.
/// </summary>
public interface ITypeClassifier
{
    /// <summary>
    /// Determines if a type symbol represents a ValueObject.
    /// </summary>
    bool IsValueObjectType(ITypeSymbol? typeSymbol);

    /// <summary>
    /// Determines if a type symbol represents an Entity.
    /// </summary>
    bool IsEntityType(ITypeSymbol? typeSymbol);

    /// <summary>
    /// Determines if a type symbol represents an Aggregate.
    /// </summary>
    bool IsAggregateType(ITypeSymbol? typeSymbol);

    /// <summary>
    /// Gets the underlying type of ValueObject.
    /// </summary>
    ITypeSymbol? GetValueObjectUnderlyingType(INamedTypeSymbol namedType);
}
