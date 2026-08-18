namespace Majal.Generators.Dtos.Models;

public readonly record struct ForwardArgument(
    string DtoPropertyName,
    string SourceExpression
);