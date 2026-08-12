using Microsoft.CodeAnalysis;

namespace Majal.Generators.Dtos.Models;

internal readonly record struct ParameterContext(
    IParameterSymbol Parameter,
    DtoContext DtoContext,
    HashSet<string> ExcludedProperties,
    string? MethodXml
);