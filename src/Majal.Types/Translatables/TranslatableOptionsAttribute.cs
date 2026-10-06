using System;

namespace Majal;

/// <summary>
/// Configures Translatable generator defaults at the assembly level.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
public sealed class TranslatableOptionsAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TranslatableOptionsAttribute"/> class.
    /// </summary>
    public TranslatableOptionsAttribute() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslatableOptionsAttribute"/> class with supported locales.
    /// </summary>
    /// <param name="supportedLocales">The supported locales for the application.</param>
    public TranslatableOptionsAttribute(params string[] supportedLocales)
    {
        SupportedLocales = supportedLocales;
    }

    /// <summary>
    /// The default locale type for translatables that use the non-generic [Translatable] attribute.
    /// When set, [Translatable] will use this type instead of the default <c>string</c>.
    /// </summary>
    public Type? DefaultLocaleType { get; set; }

    /// <summary>
    /// The supported locales for the application.
    /// When specified, translatable extension methods will be generated for locale validation.
    /// </summary>
    public string[]? SupportedLocales { get; set; }

    /// <summary>
    /// Optional namespace for the generated <c>TranslatableExtensions</c>. Defaults to <c>Majal</c>.
    /// </summary>
    public string? Namespace { get; set; }
}
