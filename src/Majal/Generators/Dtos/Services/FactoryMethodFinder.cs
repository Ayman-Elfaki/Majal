using Majal.Common.Abstractions;
using Microsoft.CodeAnalysis;

namespace Majal.Generators.Dtos.Services;

/// <summary>
/// Default implementation of factory method finding.
/// </summary>
public sealed class FactoryMethodFinder : IFactoryMethodFinder
{
    public IMethodSymbol? FindFactoryMethod(INamedTypeSymbol symbol, string factoryMethodName)
    {
        for (var current = symbol; current != null; current = current.BaseType)
        {
            var createMethod = current.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m => SymbolEqualityComparer.Default.Equals(m.ReturnType, symbol))
                .FirstOrDefault(m => m.IsStatic && m.Name == factoryMethodName);

            if (createMethod != null) return createMethod;
        }

        return null;
    }

    public List<IMethodSymbol> FindFactoryMethodsInDerivedTypes(INamedTypeSymbol symbol, string methodName, Compilation compilation)
    {
        var methods = new List<IMethodSymbol>();
        var allTypes = compilation.GetAllTypesInCompilation().ToArray();

        foreach (var derivedType in allTypes)
        {
            if (!derivedType.IsSymbolDerivedFrom(symbol)) continue;
            if (FindFactoryMethod(derivedType, methodName) is { } method) methods.Add(method);
        }

        return methods;
    }
}
