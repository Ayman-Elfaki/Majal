using System;

namespace Majal;

/// <summary>
/// Ignores a property when generating DTO properties or mapping.
/// Can be applied to a DTO class/record (specifying the property name),
/// or directly to a domain entity property or field.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
public sealed class DtoIgnoreAttribute : Attribute
{
    /// <summary>
    /// Gets the property name to ignore when applied to a DTO.
    /// </summary>
    public string? PropertyName { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="DtoIgnoreAttribute"/> for use on an entity property or field.
    /// </summary>
    public DtoIgnoreAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="DtoIgnoreAttribute"/> specifying the property name to ignore on the DTO.
    /// </summary>
    /// <param name="propertyName">The property name or path to ignore.</param>
    public DtoIgnoreAttribute(string propertyName)
    {
        PropertyName = propertyName;
        PropertyNames = [propertyName];
    }

    /// <summary>
    /// Initializes a new instance of <see cref="DtoIgnoreAttribute"/> specifying several property names to ignore on the DTO.
    /// </summary>
    /// <param name="propertyNames">The property names or paths to ignore.</param>
    public DtoIgnoreAttribute(params string[] propertyNames)
    {
        PropertyNames = propertyNames;
        PropertyName = propertyNames.Length > 0 ? propertyNames[0] : null;
    }

    /// <summary>
    /// Gets all property names to ignore when applied to a DTO.
    /// </summary>
    public string[] PropertyNames { get; } = [];
}

