namespace Majal.Generators.Dtos.Models;

public readonly record struct DerivedTypeInfo(
    string DtoName,
    string Discriminator
);