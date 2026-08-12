namespace Majal.Generators.Dtos.Models;

public readonly record struct FlattenedArgument(
    string SubFactoryParameterName,
    string DtoPropertyName
);