using System.Globalization;

namespace Majal;

/// <summary>
/// Defines the interface for a database context that supports translatable entities.
/// </summary>
public interface ITranslatableDbContext<out TLocale>
{
    /// <summary>
    /// Gets the current locale used for translations.
    /// </summary>
    TLocale Locale { get; }
}