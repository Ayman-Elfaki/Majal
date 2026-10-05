namespace Majal;

/// <summary>
/// Strategy for matching property names between domain models and DTOs.
/// </summary>
public enum NameMatchingStrategy
{
    /// <summary>
    /// Exact ordinal case-sensitive match.
    /// </summary>
    Exact,

    /// <summary>
    /// Case-insensitive match.
    /// </summary>
    IgnoreCase,

    /// <summary>
    /// Flexible match (ignores case, underscores, and casing style).
    /// </summary>
    Flexible
}
