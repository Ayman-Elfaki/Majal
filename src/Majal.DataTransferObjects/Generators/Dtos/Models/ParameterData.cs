namespace Majal.Generators.Dtos.Models;

public readonly record struct ParameterData(
    (string Name, string Type) Declaration,
    bool IsNullable,
    string? XmlDocs = null
);