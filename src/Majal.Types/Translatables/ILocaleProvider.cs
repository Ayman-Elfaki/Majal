namespace Majal;

/// <summary>
/// Defines the interface for a provider that returns the current locale.
/// </summary>
public interface ILocaleProvider<out TLocale>
{
    /// <summary>
    /// Gets the current locale identifier.
    /// </summary>
    TLocale GetCurrentLocale();
}