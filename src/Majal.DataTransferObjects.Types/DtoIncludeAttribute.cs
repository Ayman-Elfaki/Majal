using System;

namespace Majal;

/// <summary>
/// Specifies entity properties (such as <c>Id</c>, <c>CreatedOn</c>, or <c>Ordinal</c>) that should be
/// included on the generated DTO and mapped during forward mapping (<c>From</c> and <c>Projection</c>),
/// even if they are not parameters of the domain type's factory method.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
public sealed class DtoIncludeAttribute : Attribute
{
    /// <summary>
    /// Gets the names of the entity properties to include on the DTO.
    /// </summary>
    public string[] Properties { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DtoIncludeAttribute"/> class.
    /// </summary>
    /// <param name="properties">The property names to include on the DTO.</param>
    public DtoIncludeAttribute(params string[] properties)
    {
        Properties = properties ?? [];
    }
}
