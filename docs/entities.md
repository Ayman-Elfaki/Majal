# Entities Guide

An **Entity** is an object that is defined by its identity rather than its attributes. Even if two entities have the same property values, they are considered different if they have different IDs.

Majal provides the `[Entity<TId>]` attribute to automate the identity management and equality logic for your entities.

## Usage

Mark your entity class with the `[Entity<TId>]` attribute, specifying the type of the identifier (e.g., `int`, `Guid`, `long`). The class must be `partial`.

```csharp
using Majal;

namespace MyProject.Domain;

[Entity]
public partial class Product
{
    public required string Name { get; init; }
    public decimal Price { get; set; }
}
```

## Generated Code

The `[Entity]` generator produces a partial class that:

1.  **Implements `IEntity<TId>`**: Provides a standardized way to access the entity's ID.
2.  **Generates an `Id` Property**: If you haven't defined an `Id` property, the generator will add one of type `TId`.
3.  **Implements Equality**:
    *   Overrides `Equals(object obj)` to compare entities based on their IDs and types.
    *   Implements `==` and `!=` operators.
    *   Overrides `GetHashCode()` based on the ID.
    *   Includes a "transient" check (an entity is transient if its ID is the default value for its type, typically meaning it hasn't been persisted yet).
4.  **Implements Comparison**: Implements `IComparable` and `IComparable<TEntity>` based on the ID.

### Example of Generated Code Structure

```csharp
public partial class Product : global::Majal.IEntity<Guid>, global::System.IComparable, global::System.IComparable<Product>
{
    public Guid Id { get; init; }

    public override bool Equals(object? obj) { /* ... */ }
    public static bool operator ==(Product a, Product b) { /* ... */ }
    public static bool operator !=(Product a, Product b) { /* ... */ }
    public override int GetHashCode() { /* ... */ }
    public int CompareTo(Product? other) { /* ... */ }
    // ...
}
```

## Benefits

*   **Identifiable Objects**: Ensures entities are correctly identified and compared by ID, not by value.
*   **Boilerplate-Free Equality**: Automatically handles the complex logic of identity-based equality, including null checks and transient object handling.
*   **Easy Sorting**: Built-in `IComparable` implementation allows entities to be easily sorted by their identifiers.
