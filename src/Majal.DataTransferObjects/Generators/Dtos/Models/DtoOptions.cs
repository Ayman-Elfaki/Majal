namespace Majal.Generators.Dtos.Models;

internal readonly record struct DtoOptions(
    string FactoryMethodName,
    string DtoSuffix,
    string DtoPrefix,
    string[] ExcludedProperties,
    string[] NullableProperties);