using System;

namespace Majal;

/// <summary>
/// Marks a class as having an ordinal position, enabling ordering functionality.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class OrdinalAttribute : Attribute
{
}