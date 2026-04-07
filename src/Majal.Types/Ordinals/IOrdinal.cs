
namespace Majal;

/// <summary>
/// Defines the base interface for entities that have an ordinal position.
/// </summary>
public interface IOrdinal
{
    /// <summary>
    /// Gets the ordinal position of the entity.
    /// </summary>
    uint Ordinal { get; }
}