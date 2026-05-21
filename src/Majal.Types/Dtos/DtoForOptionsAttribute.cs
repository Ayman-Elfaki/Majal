using System;

namespace Majal;

/// <summary>
/// Sets assembly-level defaults for DTO generation.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class DtoForOptionsAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the default name of the static factory method used to derive DTO properties.
    /// Defaults to "Create".
    /// </summary>
    public string FactoryMethodName { get; set; } = "Create";

    /// <summary>
    /// The generated Dto suffix
    /// </summary>
    public string Suffix { get; set; } = "Dto";

    /// <summary>
    /// The generated Dto prefix
    /// </summary>
    public string? Prefix { get; set; }

    /// <summary>
    /// Gets or sets default DTO property names to exclude from generated DTOs.
    /// </summary>
    public string[] Exclude { get; set; } = [];
}