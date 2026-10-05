using System;

namespace Majal;

/// <summary>
/// Marks a class as an entity and specifies the type of its unique identifier.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class EntityAttribute<TId> : Attribute
{
    /// <summary>
    /// Gets or sets whether foreign key properties are automatically generated for navigation properties.
    /// When specified, overrides the assembly-level <see cref="EntityOptionsAttribute.GenerateForeignKeys"/> setting.
    /// </summary>
    public bool GenerateForeignKeys { get; set; } = true;
}


/// <summary>
/// Marks a class as an entity.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class EntityAttribute : Attribute
{
    /// <summary>
    /// Gets or sets whether foreign key properties are automatically generated for navigation properties.
    /// When specified, overrides the assembly-level <see cref="EntityOptionsAttribute.GenerateForeignKeys"/> setting.
    /// </summary>
    public bool GenerateForeignKeys { get; set; } = true;
}