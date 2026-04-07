using System;

namespace Majal;

/// <summary>
/// Marks a class as a value object.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ValueObjectAttribute : Attribute
{
}


/// <summary>
/// Marks a class as a value object and specifies the type of its value.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ValueObjectAttribute<TValue> : Attribute
{
}