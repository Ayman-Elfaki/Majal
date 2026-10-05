using System;

namespace Majal;

/// <summary>
/// Configures mapping and behavior for a specific DTO property.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
public sealed class DtoMemberAttribute : Attribute
{
    /// <summary>
    /// Gets the name of the DTO property being configured.
    /// </summary>
    public string PropertyName { get; }

    /// <summary>
    /// Specifies the entity member from which this DTO property is mapped.
    /// </summary>
    public string? MapFrom { get; set; }

    /// <summary>
    /// If true, marks the generated DTO property as nullable.
    /// </summary>
    public bool Nullable { get; set; }

    /// <summary>
    /// Specifies a converter type containing a static Convert method for custom mapping.
    /// </summary>
    public Type? Using { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="DtoMemberAttribute"/> for the specified property name.
    /// </summary>
    /// <param name="propertyName">The name of the DTO property.</param>
    public DtoMemberAttribute(string propertyName)
    {
        PropertyName = propertyName;
    }
}
