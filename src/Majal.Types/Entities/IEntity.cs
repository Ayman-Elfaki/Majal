namespace Majal;

/// <summary>
/// Defines the base interface for an entity with a unique identifier.
/// </summary>
public interface IEntity<out TId>
{
    /// <summary>
    /// Gets the unique identifier of the entity.
    /// </summary>
    TId Id { get; }
}