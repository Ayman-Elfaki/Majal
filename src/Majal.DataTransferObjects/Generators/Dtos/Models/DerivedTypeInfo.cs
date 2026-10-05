using Majal.Common.Abstractions;

namespace Majal.Generators.Dtos.Models;

public readonly record struct DerivedTypeInfo(
    string DtoName,
    string Discriminator,
    string? SourceTypeName = null,
    EquatableList<SourceMemberMap>? ForwardMappings = null
);