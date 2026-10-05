using Microsoft.CodeAnalysis;

namespace Majal.Generators.Dtos.Models;

internal readonly record struct DtoTypeConfig(
    Dictionary<string, bool>? FlattenConfigs,
    ITypeSymbol[]? ExcludedTypes,
    Dictionary<string, string[]>? ExcludedTypeProperties,
    string[] ExcludedProperties,
    string[] NullableProperties,
    Dictionary<string, DtoMemberConfig>? MemberConfigs,
    string[]? IncludedProperties = null
);