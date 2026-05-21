using Microsoft.CodeAnalysis;

namespace Majal.Generators.Dtos.Services;

/// <summary>
/// Handles derivation logic for abstract types and their derived implementations.
/// Provides extensibility for custom derivation resolution.
/// </summary>
public interface IDerivationHandler
{
    /// <summary>
    /// Processes derived types for an abstract source symbol.
    /// Returns true if derived types were found and processed.
    /// </summary>
    bool TryProcessDerivedTypes(
        INamedTypeSymbol sourceSymbol,
        string factoryMethodName,
        string defaultMethodName,
        Compilation compilation,
        DtoForGenerator.DtoContext context,
        out DtoForGenerator.DtoData? resultData
    );
}
