using Microsoft.CodeAnalysis;

namespace Majal.Generators.Dtos.Services;

/// <summary>
/// Finds factory methods in type symbols.
/// Provides extensibility for custom factory method resolution.
/// </summary>
public interface IFactoryMethodFinder
{
    /// <summary>
    /// Finds a factory method by name in a symbol and its base types.
    /// </summary>
    IMethodSymbol? FindFactoryMethod(INamedTypeSymbol symbol, string factoryMethodName);

    /// <summary>
    /// Finds factory methods in all derived types of a symbol.
    /// </summary>
    List<IMethodSymbol> FindFactoryMethodsInDerivedTypes(INamedTypeSymbol symbol, string methodName, Compilation compilation);
}
