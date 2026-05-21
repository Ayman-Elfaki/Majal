using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Majal.Generators.Dtos.Services;

/// <summary>
/// Builds DtoContext from attribute syntax context and related configuration.
/// Provides extensibility for custom context initialization.
/// </summary>
public interface IDtoContextBuilder
{
    /// <summary>
    /// Builds a DtoContext from generator attribute syntax context.
    /// </summary>
    DtoForGenerator.DtoContext? BuildDtoContext(
        GeneratorAttributeSyntaxContext attributeContext,
        INamedTypeSymbol dtoSymbol,
        INamedTypeSymbol sourceSymbol,
        string attributeNamePrefix,
        string attributeNameSuffix,
        string factoryMethodName,
        string defaultMethodName
    );
}
