using System;

namespace Majal;

/// <summary>
/// Marks a class or record as a DTO for the specified type.
/// The DTO properties will be generated based on the target type's specified factory method parameters.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class DtoForAttribute<T> : Attribute
{
    /// <summary>
    /// Gets or sets the name of the static factory method used to derive DTO properties.
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
    /// Name matching strategy for mapping between domain entity members and DTO properties.
    /// Defaults to <see cref="NameMatchingStrategy.Exact"/>.
    /// </summary>
    public NameMatchingStrategy NameMatching { get; set; } = NameMatchingStrategy.Exact;

    /// <summary>
    /// Controls which mapping methods are generated (ToDto, To, or TwoWay).
    /// Defaults to <see cref="MapDirection.TwoWay"/>.
    /// </summary>
    public MapDirection Directions { get; set; } = MapDirection.TwoWay;
}