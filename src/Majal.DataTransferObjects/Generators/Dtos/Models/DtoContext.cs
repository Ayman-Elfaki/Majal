using Microsoft.CodeAnalysis;

namespace Majal.Generators.Dtos.Models;

internal readonly record struct DtoContext(
    string Namespace,
    string DtoName,
    string RawDtoName,
    string[] ParentTypeDeclarations,
    string DtoNamePrefix,
    string DtoNameSuffix,
    Accessibility Accessibility,
    INamedTypeSymbol SourceSymbol,
    bool IsRoot,
    bool IsRecord,
    string FactoryMethodName,
    DtoGraph Graph,
    Dictionary<string, bool>? FlattenConfigs = null,
    ITypeSymbol[]? ExcludedTypes = null,
    string[]? ExcludedProperties = null!,
    Dictionary<string, string[]>? ExcludedTypeProperties = null,
    string[]? NullableProperties = null!,
    Compilation? Compilation = null
);