using System;

namespace Majal;

/// <summary>
/// Marks a class as translatable, enabling multi-language support.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TranslatableAttribute : Attribute;

/// <summary>
/// Marks a class as translatable, enabling multi-language support with custom locale type.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TranslatableAttribute<TLocale> : Attribute;