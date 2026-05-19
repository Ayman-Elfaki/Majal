using System;

namespace Majal;

/// <summary>
/// Marks a class or struct as a DTO for the specified type.
/// The DTO properties will be generated based on the target type's specified factory method parameters.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class DtoForAttribute<T> : Attribute
{
    /// <summary>
    /// Gets or sets the name of the static factory method used to derive DTO properties.
    /// Defaults to "Create".
    /// </summary>
    public string FactoryMethodName { get; set; } = "Create";

    /// <summary>
    /// The generated Dto suffix
    /// </summary>
    public string DtoSuffix { get; set; } = "Dto";
}