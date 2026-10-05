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
    NameMatchingStrategy NameMatching = NameMatchingStrategy.Exact,
    MapDirection Directions = MapDirection.TwoWay,
    Dictionary<string, DtoMemberConfig>? MemberConfigs = null,
    Dictionary<string, bool>? FlattenConfigs = null,
    ITypeSymbol[]? ExcludedTypes = null,
    string[]? ExcludedProperties = null!,
    Dictionary<string, string[]>? ExcludedTypeProperties = null,
    string[]? NullableProperties = null!,
    string[]? IncludedProperties = null,
    INamedTypeSymbol? DtoSymbol = null,
    Compilation? Compilation = null
);