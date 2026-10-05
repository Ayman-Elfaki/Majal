using System;

namespace Majal;

/// <summary>
/// Marks a referenced type to be excluded from generated DTOs when referenced by a DtoFor target.
/// </summary>
/// <typeparam name="T">The type to exclude from DTO generation.</typeparam>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
public sealed class DtoIgnoreTypeAttribute<T> : Attribute
{
}
