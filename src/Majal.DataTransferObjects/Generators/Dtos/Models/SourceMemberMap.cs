namespace Majal.Generators.Dtos.Models;

public readonly record struct SourceMemberMap(
    string DtoPropertyName,
    string DtoPropertyType,
    string AdaptExpression,
    string ProjectionExpression,
    bool IsNullable = false
);
