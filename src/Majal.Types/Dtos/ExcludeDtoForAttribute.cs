using System;

namespace Majal;

/// <summary>
/// Marks a type to be excluded from generated DTOs when referenced by a DtoFor target.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class ExcludeDtoForAttribute<T> : Attribute
{
}
