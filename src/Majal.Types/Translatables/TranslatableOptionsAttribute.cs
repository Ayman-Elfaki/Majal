using System;

namespace Majal;

/// <summary>
/// Configures Translatable generator defaults at the assembly level.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class TranslatableOptionsAttribute : Attribute
{
    /// <summary>
    /// The default locale type for translatables that use the non-generic [Translatable] attribute.
    /// When set, [Translatable] will use this type instead of the default <c>string</c>.
    /// </summary>
    public Type? DefaultLocaleType { get; set; }
}
