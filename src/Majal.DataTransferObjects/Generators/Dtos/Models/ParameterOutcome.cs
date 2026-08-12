namespace Majal.Generators.Dtos.Models;

internal readonly record struct ParameterOutcome(
    ParameterData[] Properties,
    FactoryArgument? Reconstruction
);