using System;

namespace Majal;

/// <summary>
/// Configures flattening for a specific nested type (DTO or ValueObject) within the parent DTO.
/// </summary>
/// <typeparam name="T">The type to flatten.</typeparam>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
public sealed class DtoFlattenAttribute<T> : Attribute
{
    /// <summary>
    /// Gets or sets a value indicating whether the naming order of flattened properties is reversed.
    /// When false (default), the parent parameter name is prefixed (e.g. moneyAmount).
    /// When true, the inner property name is prefixed (e.g. amountMoney).
    /// </summary>
    public bool IsReversed { get; set; }
}
