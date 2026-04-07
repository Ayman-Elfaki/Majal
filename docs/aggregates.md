# Aggregates Guide

In Domain-Driven Design, an **Aggregate** is a cluster of domain objects that can be treated as a single unit. Every aggregate has a single **Aggregate Root**, which is the only member of the aggregate that outside objects are allowed to hold a reference to.

Majal provides the `[Aggregate<TDomainEvent>]` attribute to automate the common infrastructure needed for aggregates, specifically around **Domain Events**.

## Usage

Mark your aggregate root class with the `[Aggregate<TDomainEvent>]` attribute. The class must be `partial`.

```csharp
using Majal;

namespace MyProject.Domain;

public record UserEvent; // Base class for domain events

[Entity]
[Aggregate<UserEvent>]
public partial class User
{
    public required string Username { get; init; }

    public void ChangeUsername(string newUsername)
    {
        // ... logic ...
        Publish(new UserEvent()); // Using the generated Publish method
    }
}
```

## Generated Code

The `[Aggregate]` generator produces a partial class that:

1.  **Implements `IAggregate<TDomainEvent>`**: This is a marker interface that identifies the class as an aggregate root.
2.  **Manages Domain Events**:
    *   `Events`: An `IEnumerable<TDomainEvent>` property that exposes the collection of domain events that have occurred within the aggregate.
    *   `Publish(TDomainEvent @event)`: A method to record a new domain event.
    *   `Clear()`: A method to clear the collection of domain events (typically called after the events have been dispatched).

### Example of Generated Code Structure

```csharp
public partial class User : global::Majal.IAggregate<UserEvent>
{
    private readonly global::System.Collections.Generic.List<UserEvent> _events = [];

    public global::System.Collections.Generic.IEnumerable<UserEvent> Events => _events;

    public void Publish(UserEvent @event)
    {
        _events.Add(@event);
    }

    public void Clear()
    {
        _events.Clear();
    }
}
```

## Benefits

*   **Standardized Event Handling**: Ensures all aggregate roots handle domain events in a consistent way.
*   **Reduced Boilerplate**: Removes the need to manually implement the event collection and helper methods in every aggregate root.
*   **Type Safety**: The generic parameter `<TDomainEvent>` ensures that only events of the correct type can be published.
