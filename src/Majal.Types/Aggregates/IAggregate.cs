using System.Collections.Generic;

namespace Majal;

/// <summary>
/// Defines the base interface for an aggregate that publishes domain events.
/// </summary>
public interface IAggregate<TDomainEvent>
{
    /// <summary>
    /// Gets the collection of published domain events.
    /// </summary>
    IEnumerable<TDomainEvent> Events { get; }

    /// <summary>
    /// Adds a domain event to the collection of published events.
    /// </summary>
    void Publish(TDomainEvent @event);

    /// <summary>
    /// Clears the collection of published domain events.
    /// </summary>
    void Clear();
}