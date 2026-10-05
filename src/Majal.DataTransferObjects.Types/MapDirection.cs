using System;

namespace Majal;

/// <summary>
/// Specifies the direction of generated mapping methods.
/// </summary>
[Flags]
public enum MapDirection
{
    /// <summary>
    /// Do not generate mapping methods.
    /// </summary>
    None = 0,

    /// <summary>
    /// Generate entity-to-DTO mapping (FromEntity/FromValueObject, their OrDefault variants, and Projection).
    /// </summary>
    ToDto = 1,

    /// <summary>
    /// Generate DTO-to-entity mapping (ToEntity/ToValueObject).
    /// </summary>
    ToEntity = 2,

    /// <summary>
    /// Alias for <see cref="ToEntity"/>.
    /// </summary>
    To = 2,

    /// <summary>
    /// Generate both forward (ToDto) and reverse (To) mappings.
    /// </summary>
    TwoWay = ToDto | ToEntity
}
