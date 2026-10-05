namespace Majal.Generators.Dtos.Models;

internal readonly record struct DtoOptions(
    string FactoryMethodName,
    string DtoSuffix,
    string DtoPrefix,
    NameMatchingStrategy NameMatching,
    MapDirection Directions
);