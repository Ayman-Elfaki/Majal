using System;

namespace Majal;

/// <summary>
/// Configures Entity generator defaults at the assembly level.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class EntityOptionsAttribute : Attribute
{
    /// <summary>
    /// The default ID type for entities that use the non-generic [Entity] attribute.
    /// When set, [Entity] will use this type instead of the default <c>int</c>.
    /// </summary>
    public Type? DefaultIdType { get; set; }

    /// <summary>
    /// Gets or sets whether foreign key properties are automatically generated for navigation properties.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool GenerateForeignKeys { get; set; } = true;
}
