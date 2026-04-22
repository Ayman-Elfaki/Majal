namespace Majal;

/// <summary>
/// Defines the base interface for translatable entities.
/// </summary>
public interface ITranslatable<out TLocale>
{
    /// <summary>
    /// Gets the locale of the translation.
    /// </summary>
    TLocale Locale { get; }
}