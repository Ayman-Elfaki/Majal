using Microsoft.CodeAnalysis;

namespace Majal.Generators.Dtos.Services;

/// <summary>
/// Builds and resolves DTO parameters from factory method signatures.
/// Provides extensibility for custom parameter resolution strategies.
/// </summary>
public interface IParameterBuilder
{
    /// <summary>
    /// Builds a collection of parameters from a factory method with context awareness.
    /// </summary>
    List<DtoForGenerator.ParameterData> BuildParameters(
        IMethodSymbol createMethod,
        ITypeSymbol sourceType,
        string defaultMethodName,
        string dtoNamePrefix,
        string dtoNameSuffix,
        Accessibility accessibility,
        bool isRecord,
        string @namespace,
        Dictionary<string, DtoForGenerator.DtoData> collected,
        string[] parentTypeDeclarations,
        Dictionary<string, bool>? flattenConfigs,
        Compilation? compilation
    );
}
