using System;

namespace Majal;

/// <summary>
/// Configures flattening for a specific nested type (DTO or ValueObject) within the parent DTO.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class FlattenDtoForAttribute<T> : Attribute
{
    /// <summary>
    /// Gets or sets a value indicating whether the naming order of flattened properties is reversed.
    /// When false (default), the parent parameter name is prefixed (e.g. moneyAmount).
    /// When true, the inner property name is prefixed (e.g. amountMoney).
    /// </summary>
    public bool IsReversed { get; set; }
}