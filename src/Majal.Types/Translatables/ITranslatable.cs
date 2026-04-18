namespace Majal;

/// <summary>
/// Defines the base interface for translatable entities.
/// </summary>
public interface ITranslatable
{
    /// <summary>
    /// Gets the locale of the translation.
    /// </summary>
    string? Locale { get; }
}