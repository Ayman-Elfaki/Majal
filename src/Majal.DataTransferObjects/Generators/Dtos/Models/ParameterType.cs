using Microsoft.CodeAnalysis;

namespace Majal.Generators.Dtos.Models;

internal readonly record struct ParameterType(
    ITypeSymbol? Type,
    bool IsNullable,
    ParameterContext Context
);