using System;

namespace Majal;

/// <summary>
/// Marks a class as translatable, enabling multi-language support.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TranslatableAttribute : Attribute
{
}