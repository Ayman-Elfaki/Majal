using Microsoft.CodeAnalysis;

namespace Majal.Generators.Dtos.Services;

/// <summary>
/// Orchestrates the complete DTO data generation process.
/// Provides extensibility by delegating to specialized services.
/// </summary>
public interface IDtoDataBuilder
{
    /// <summary>
    /// Builds complete DTO data from a DTO context.
    /// Returns null if the DTO cannot be built from the given context.
    /// </summary>
    DtoForGenerator.DtoData? BuildDtoData(DtoForGenerator.DtoContext context);
}
