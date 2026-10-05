namespace Majal.Generators.Dtos.Models;

public readonly record struct DtoMemberConfig(
    string PropertyName,
    string? MapFrom = null,
    bool Nullable = false,
    string? UsingTypeName = null
);
