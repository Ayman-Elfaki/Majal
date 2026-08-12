using Majal.Common.Abstractions;

namespace Majal.Generators.Dtos.Models;

public readonly record struct FactoryArgument(
    string FactoryParameterName,
    ReconstructKind Kind,
    string? DtoPropertyName = null,
    string? TargetTypeName = null,
    bool IsCollection = false,
    string CollectionConversionKind = "",
    bool IsNullable = false,
    EquatableList<FlattenedArgument>? FlattenedArguments = null
);