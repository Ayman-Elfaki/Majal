using System;

namespace Majal;

/// <summary>
/// Sets assembly-level defaults for DTO generation and mapping.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
public sealed class DtoConfigAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the default name of the static factory method used to derive DTO properties.
    /// Defaults to "Create".
    /// </summary>
    public string FactoryMethod { get; set; } = "Create";

    /// <summary>
    /// The generated DTO suffix. Defaults to "Dto".
    /// </summary>
    public string Suffix { get; set; } = "Dto";

    /// <summary>
    /// The generated DTO prefix.
    /// </summary>
    public string? Prefix { get; set; }

    /// <summary>
    /// Default name matching strategy. Defaults to <see cref="NameMatchingStrategy.Exact"/>.
    /// </summary>
    public NameMatchingStrategy NameMatching { get; set; } = NameMatchingStrategy.Exact;

    /// <summary>
    /// Default mapping direction. Defaults to <see cref="MapDirection.TwoWay"/>.
    /// </summary>
    public MapDirection Directions { get; set; } = MapDirection.TwoWay;
}
