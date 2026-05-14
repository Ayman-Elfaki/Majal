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
}
