namespace Majal;

/// <summary>
/// Defines the interface for a database context that supports translatable entities.
/// </summary>
public interface ITranslatableDbContext
{
    /// <summary>
    /// Gets the current locale used for translations.
    /// </summary>
    string CurrentLocale { get; }
}