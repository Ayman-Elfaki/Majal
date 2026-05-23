using System;

namespace Majal;

/// <summary>
/// Marks a type to be excluded from generated DTOs when referenced by a DtoFor target.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class ExcludeDtoForAttribute<T> : Attribute
{
    /// <summary>
    /// Gets or sets DTO property names to exclude from generated DTOs for the specified type.
    /// If provided, only the listed properties are excluded and the referenced type is still generated as a nested DTO.
    /// </summary>
    public string[] Properties { get; set; } = [];
}
