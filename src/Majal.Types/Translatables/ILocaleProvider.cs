namespace Majal;

/// <summary>
/// Defines the interface for a provider that returns the current locale.
/// </summary>
public interface ILocaleProvider
{
    /// <summary>
    /// Gets the current locale identifier.
    /// </summary>
    string GetCurrentLocale();
}